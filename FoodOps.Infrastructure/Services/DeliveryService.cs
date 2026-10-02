using FoodOps.Application.Common;
using FoodOps.Application.DTOs.Delivery;
using FoodOps.Application.DTOs.Orders;
using FoodOps.Application.Interfaces;
using FoodOps.Domain.Entities;
using FoodOps.Domain.Enums;
using FoodOps.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace FoodOps.Infrastructure.Services;

public class DeliveryService : IDeliveryService
{
    private readonly AppDbContext _db;
    public DeliveryService(AppDbContext db) => _db = db;

    private static readonly OrderStatus[] Assignable =
        { OrderStatus.Accepted, OrderStatus.Preparing, OrderStatus.ReadyForPickup };

    public async Task<RiderDto> SetAvailabilityAsync(Guid userId, string fullName, SetAvailabilityDto dto)
    {
        var rider = await _db.Riders.FirstOrDefaultAsync(r => r.UserId == userId);
        if (rider == null)
        {
            rider = new Rider { UserId = userId, Name = fullName, Phone = dto.Phone ?? string.Empty };
            _db.Riders.Add(rider);
        }
        else if (!string.IsNullOrWhiteSpace(dto.Phone))
        {
            rider.Phone = dto.Phone;
        }

        rider.IsOnline = dto.IsOnline;
        await _db.SaveChangesAsync();
        return new RiderDto(rider.Id, rider.Name, rider.Phone, rider.IsOnline);
    }

    public async Task<List<OrderDto>> GetAvailableOrdersAsync()
    {
        return await _db.Orders.AsNoTracking()
            .Where(o => o.RiderId == null && Assignable.Contains(o.Status))
            .OrderBy(o => o.OrderTime)
            .ProjectToDto()
            .ToListAsync();
    }

    public async Task<OrderDto> AssignAsync(int orderId, Guid userId)
    {
        var rider = await GetRiderAsync(userId);
        if (!rider.IsOnline)
            throw new InvalidOperationException("Go online before accepting orders.");

        // Single atomic UPDATE: if two riders click together, only one gets rows = 1
        var rows = await _db.Orders
            .Where(o => o.Id == orderId && o.RiderId == null && Assignable.Contains(o.Status))
            .ExecuteUpdateAsync(s => s.SetProperty(o => o.RiderId, rider.Id));

        if (rows == 0)
            throw new InvalidOperationException("Order is no longer available.");

        var order = await _db.Orders.AsNoTracking().FirstAsync(o => o.Id == orderId);

        _db.Deliveries.Add(new Delivery { OrderId = orderId, RiderId = rider.Id });
        _db.OrderStatusLogs.Add(new OrderStatusLog
        {
            OrderId = orderId,
            OldStatus = order.Status,
            NewStatus = order.Status,
            ChangedBy = userId,
            Remarks = $"Rider {rider.Name} assigned"
        });
        await _db.SaveChangesAsync();

        return await _db.Orders.AsNoTracking().Where(o => o.Id == orderId).ProjectToDto().FirstAsync();
    }

    public async Task<OrderDto> UpdateStatusAsync(UpdateDeliveryStatusDto dto, Guid userId)
    {
        var rider = await GetRiderAsync(userId);
        var order = await _db.Orders.FirstOrDefaultAsync(o => o.Id == dto.OrderId)
            ?? throw new NotFoundException("Order not found.");

        if (order.RiderId != rider.Id)
            throw new ForbiddenException("This order is not assigned to you.");

        var old = order.Status;
        var valid = (old == OrderStatus.ReadyForPickup && dto.NewStatus == OrderStatus.PickedUp)
                 || (old == OrderStatus.PickedUp && dto.NewStatus == OrderStatus.Delivered);
        if (!valid)
            throw new InvalidOperationException($"Cannot move order from {old} to {dto.NewStatus}.");

        var delivery = await _db.Deliveries.FirstOrDefaultAsync(d => d.OrderId == order.Id)
            ?? throw new NotFoundException("Delivery record not found.");

        if (dto.NewStatus == OrderStatus.PickedUp) delivery.PickupTime = DateTime.UtcNow;
        else delivery.DeliveredTime = DateTime.UtcNow;

        order.Status = dto.NewStatus;
        _db.OrderStatusLogs.Add(new OrderStatusLog
        {
            OrderId = order.Id,
            OldStatus = old,
            NewStatus = dto.NewStatus,
            ChangedBy = userId
        });
        await _db.SaveChangesAsync();

        return await _db.Orders.AsNoTracking().Where(o => o.Id == order.Id).ProjectToDto().FirstAsync();
    }

    public async Task<List<DeliveryDto>> GetMyDeliveriesAsync(Guid userId)
    {
        var rider = await GetRiderAsync(userId);
        return await _db.Deliveries.AsNoTracking()
            .Where(d => d.RiderId == rider.Id)
            .Join(_db.Orders, d => d.OrderId, o => o.Id, (d, o) => new { d, o })
            .OrderByDescending(x => x.d.Id)
            .Select(x => new DeliveryDto(x.d.Id, x.d.OrderId, x.d.RiderId,
                x.d.PickupTime, x.d.DeliveredTime, x.o.Status))
            .ToListAsync();
    }

    private async Task<Rider> GetRiderAsync(Guid userId) =>
        await _db.Riders.FirstOrDefaultAsync(r => r.UserId == userId)
            ?? throw new InvalidOperationException("Set yourself online first to create your rider profile.");
}