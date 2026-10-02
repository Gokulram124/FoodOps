using FoodOps.Application.Common;
using FoodOps.Application.DTOs.Orders;
using FoodOps.Application.Interfaces;
using FoodOps.Domain.Entities;
using FoodOps.Domain.Enums;
using FoodOps.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace FoodOps.Infrastructure.Services;

public class OrderService : IOrderService
{
    private readonly AppDbContext _db;
    public OrderService(AppDbContext db) => _db = db;

    // Which status the restaurant side is allowed to move an order to
    private static readonly Dictionary<OrderStatus, OrderStatus[]> RestaurantFlow = new()
    {
        [OrderStatus.Placed] = new[] { OrderStatus.Accepted, OrderStatus.Rejected },
        [OrderStatus.Accepted] = new[] { OrderStatus.Preparing },
        [OrderStatus.Preparing] = new[] { OrderStatus.ReadyForPickup }
    };

    public async Task<OrderDto> PlaceOrderAsync(CreateOrderDto dto, Guid customerId)
    {
        if (dto.Items == null || dto.Items.Count == 0)
            throw new InvalidOperationException("Order must have at least one item.");
        if (dto.Items.Any(i => i.Quantity <= 0))
            throw new InvalidOperationException("Quantity must be greater than zero.");

        var restaurant = await _db.Restaurants.AsNoTracking().FirstOrDefaultAsync(r => r.Id == dto.RestaurantId)
            ?? throw new NotFoundException("Restaurant not found.");
        if (!restaurant.IsOpen)
            throw new InvalidOperationException("Restaurant is closed.");

        var requested = dto.Items.GroupBy(i => i.MenuItemId)
            .ToDictionary(g => g.Key, g => g.Sum(x => x.Quantity));
        var ids = requested.Keys.ToList();

        var menu = await _db.MenuItems
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

        _db.Orders.Add(order);
        await _db.SaveChangesAsync();

        _db.OrderStatusLogs.Add(new OrderStatusLog
        {
            OrderId = order.Id,
            OldStatus = OrderStatus.Placed,
            NewStatus = OrderStatus.Placed,
            ChangedBy = customerId,
            Remarks = "Order placed"
        });
        await _db.SaveChangesAsync();

        return await _db.Orders.AsNoTracking().Where(o => o.Id == order.Id).ProjectToDto().FirstAsync();
    }

    public async Task<List<OrderDto>> GetMyOrdersAsync(Guid customerId)
    {
        return await _db.Orders.AsNoTracking()
            .Where(o => o.CustomerId == customerId)
            .OrderByDescending(o => o.OrderTime)
            .ProjectToDto()
            .ToListAsync();
    }

    public async Task<List<OrderDto>> GetRestaurantOrdersAsync(Guid userId, bool isAdmin, OrderStatus? status)
    {
        var query = _db.Orders.AsNoTracking().AsQueryable();
        if (!isAdmin)
            query = query.Where(o => o.Restaurant!.OwnerId == userId);
        if (status.HasValue)
            query = query.Where(o => o.Status == status.Value);

        return await query.OrderByDescending(o => o.OrderTime).ProjectToDto().ToListAsync();
    }

    public async Task<OrderDto> UpdateStatusAsync(UpdateOrderStatusDto dto, Guid userId, string role)
    {
        var order = await _db.Orders.Include(o => o.Restaurant)
            .FirstOrDefaultAsync(o => o.Id == dto.OrderId)
            ?? throw new NotFoundException("Order not found.");

        var old = order.Status;

        if (role == Roles.Customer)
        {
            if (order.CustomerId != userId) throw new ForbiddenException("Not your order.");
            if (dto.NewStatus != OrderStatus.Cancelled || old != OrderStatus.Placed)
                throw new InvalidOperationException("You can only cancel an order that is still in Placed status.");
        }
        else if (role == Roles.RestaurantOwner || role == Roles.Admin)
        {
            if (role == Roles.RestaurantOwner && order.Restaurant!.OwnerId != userId)
                throw new ForbiddenException("Not your restaurant's order.");

            if (!RestaurantFlow.TryGetValue(old, out var allowed) || !allowed.Contains(dto.NewStatus))
                throw new InvalidOperationException($"Cannot move order from {old} to {dto.NewStatus}.");
        }
        else
        {
            throw new ForbiddenException("You are not allowed to update order status.");
        }

        order.Status = dto.NewStatus;
        _db.OrderStatusLogs.Add(new OrderStatusLog
        {
            OrderId = order.Id,
            OldStatus = old,
            NewStatus = dto.NewStatus,
            ChangedBy = userId,
            Remarks = dto.Remarks
        });
        await _db.SaveChangesAsync();

        return await _db.Orders.AsNoTracking().Where(o => o.Id == order.Id).ProjectToDto().FirstAsync();
    }

    public async Task<List<OrderStatusLogDto>> GetTimelineAsync(int orderId, Guid userId, string role)
    {
        var order = await _db.Orders.AsNoTracking().Include(o => o.Restaurant)
            .FirstOrDefaultAsync(o => o.Id == orderId)
            ?? throw new NotFoundException("Order not found.");

        var allowed = role == Roles.Admin
            || (role == Roles.Customer && order.CustomerId == userId)
            || (role == Roles.RestaurantOwner && order.Restaurant!.OwnerId == userId);
        if (!allowed) throw new ForbiddenException("You cannot view this order.");

        return await _db.OrderStatusLogs.AsNoTracking()
            .Where(l => l.OrderId == orderId)
            .OrderBy(l => l.ChangedOn)
            .Select(l => new OrderStatusLogDto(l.OldStatus, l.NewStatus, l.ChangedOn, l.Remarks))
            .ToListAsync();
    }
}