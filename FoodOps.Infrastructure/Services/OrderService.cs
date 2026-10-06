using FoodOps.Application.Common;
using FoodOps.Application.DTOs.Orders;
using FoodOps.Application.Interfaces;
using FoodOps.Application.Notifications;
using FoodOps.Application.OrderRules;
using FoodOps.Domain.Entities;
using FoodOps.Domain.Enums;
using FoodOps.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace FoodOps.Infrastructure.Services;

public class OrderService : IOrderService
{
    private readonly IUnitOfWork _uow;
    private readonly IEnumerable<IStatusTransitionRule> _rules;
    private readonly IOrderNotificationFactory _notifications;
    private readonly ILogger<OrderService> _logger;

    public OrderService(
        IUnitOfWork uow,
        IEnumerable<IStatusTransitionRule> rules,
        IOrderNotificationFactory notifications,
        ILogger<OrderService> logger)
    {
        _uow = uow;
        _rules = rules;
        _notifications = notifications;
        _logger = logger;
    }

    public async Task<OrderDto> PlaceOrderAsync(CreateOrderDto dto, Guid customerId)
    {
        if (dto.Items == null || dto.Items.Count == 0)
            throw new InvalidOperationException("Order must have at least one item.");
        if (dto.Items.Any(i => i.Quantity <= 0))
            throw new InvalidOperationException("Quantity must be greater than zero.");

        var restaurant = await _uow.Restaurants.QueryNoTracking().FirstOrDefaultAsync(r => r.Id == dto.RestaurantId)
            ?? throw new NotFoundException("Restaurant not found.");
        if (!restaurant.IsOpen)
            throw new InvalidOperationException("Restaurant is closed.");

        var requested = dto.Items.GroupBy(i => i.MenuItemId)
            .ToDictionary(g => g.Key, g => g.Sum(x => x.Quantity));
        var ids = requested.Keys.ToList();

        var menu = await _uow.MenuItems.QueryNoTracking()
            .Where(m => m.RestaurantId == dto.RestaurantId && ids.Contains(m.Id))
            .ToListAsync();

        if (menu.Count != ids.Count)
            throw new InvalidOperationException("One or more items do not belong to this restaurant.");
        if (menu.Any(m => !m.IsAvailable))
            throw new InvalidOperationException("One or more items are unavailable.");

        var order = new Order
        {
            CustomerId = customerId,
            RestaurantId = dto.RestaurantId,
            Status = OrderStatus.Placed,
            OrderTime = DateTime.UtcNow
        };

        // Price always comes from the database, never from the client
        foreach (var m in menu)
            order.Items.Add(new OrderItem { MenuItemId = m.Id, Quantity = requested[m.Id], Price = m.Price });

        order.TotalAmount = order.Items.Sum(i => i.Price * i.Quantity);

        await using var tx = await _uow.BeginTransactionAsync();

        await _uow.Orders.AddAsync(order);
        await _uow.SaveChangesAsync();   // Id generate aagum
        await _uow.OrderStatusLogs.AddAsync(new OrderStatusLog
        {
            OrderId = order.Id,
            OldStatus = OrderStatus.Placed,
            NewStatus = OrderStatus.Placed,
            ChangedBy = customerId,
            Remarks = "Order placed"
        });
        await _uow.SaveChangesAsync();

        await tx.CommitAsync();

        return await _uow.Orders.QueryNoTracking().Where(o => o.Id == order.Id).ProjectToDto().FirstAsync();

    }

    public async Task<List<OrderDto>> GetMyOrdersAsync(Guid customerId)
    {
        return await _uow.Orders.QueryNoTracking()
            .Where(o => o.CustomerId == customerId)
            .OrderByDescending(o => o.OrderTime)
            .ProjectToDto()
            .ToListAsync();
    }

    public async Task<List<OrderDto>> GetRestaurantOrdersAsync(Guid userId, bool isAdmin, OrderStatus? status)
    {
        var query = _uow.Orders.QueryNoTracking().AsQueryable();
        if (!isAdmin)
            query = query.Where(o => o.Restaurant!.OwnerId == userId);
        if (status.HasValue)
            query = query.Where(o => o.Status == status.Value);

        return await query.OrderByDescending(o => o.OrderTime).ProjectToDto().ToListAsync();
    }

    public async Task<OrderDto> UpdateStatusAsync(UpdateOrderStatusDto dto, Guid userId, string role)
    {
        var order = await _uow.Orders.Query().Include(o => o.Restaurant)
            .FirstOrDefaultAsync(o => o.Id == dto.OrderId)
            ?? throw new NotFoundException("Order not found.");

        var old = order.Status;

        var rule = _rules.FirstOrDefault(r => r.AppliesTo(role))
            ?? throw new ForbiddenException("You are not allowed to update order status.");
        rule.Validate(order, dto.NewStatus, userId, role);

        order.Status = dto.NewStatus;
        await _uow.OrderStatusLogs.AddAsync(new OrderStatusLog
        {
            OrderId = order.Id,
            OldStatus = old,
            NewStatus = dto.NewStatus,
            ChangedBy = userId,
            Remarks = dto.Remarks
        });
        await _uow.SaveChangesAsync();

        var note = _notifications.Create(order.Id, dto.NewStatus);
        if (note != null)
            _logger.LogInformation("Notify customer {CustomerId}: {Title} - {Body}",
                order.CustomerId, note.Title, note.Body);

        return await _uow.Orders.QueryNoTracking().Where(o => o.Id == order.Id).ProjectToDto().FirstAsync();
    }

    public async Task<List<OrderStatusLogDto>> GetTimelineAsync(int orderId, Guid userId, string role)
    {
        var order = await _uow.Orders.QueryNoTracking().Include(o => o.Restaurant)
            .FirstOrDefaultAsync(o => o.Id == orderId)
            ?? throw new NotFoundException("Order not found.");

        var allowed = role == Roles.Admin
            || (role == Roles.Customer && order.CustomerId == userId)
            || (role == Roles.RestaurantOwner && order.Restaurant!.OwnerId == userId);
        if (!allowed) throw new ForbiddenException("You cannot view this order.");

        return await _uow.OrderStatusLogs.QueryNoTracking()
            .Where(l => l.OrderId == orderId)
            .OrderBy(l => l.ChangedOn)
            .Select(l => new OrderStatusLogDto(l.OldStatus, l.NewStatus, l.ChangedOn, l.Remarks))
            .ToListAsync();
    }
}