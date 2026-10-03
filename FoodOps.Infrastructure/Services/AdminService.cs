using FoodOps.Application.DTOs.Admin;
using FoodOps.Application.Interfaces;
using FoodOps.Domain.Enums;
using FoodOps.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace FoodOps.Infrastructure.Services;

public class AdminService : IAdminService
{
    private readonly AppDbContext _db;
    public AdminService(AppDbContext db) => _db = db;

    // Delay-risk rule: an active order open this long is "At risk", and this long is "Delayed"
    private const int AtRiskMinutes = 30;
    private const int DelayedMinutes = 45;

    private static readonly OrderStatus[] Active =
    {
        OrderStatus.Placed, OrderStatus.Accepted, OrderStatus.Preparing,
        OrderStatus.ReadyForPickup, OrderStatus.PickedUp
    };

    public async Task<AdminStatsDto> GetStatsAsync(int tzOffsetMinutes)
    {
        var now = DateTime.UtcNow;

        // Fine for a portfolio-size dataset. At scale you would push the grouping into SQL.
        var orders = await _db.Orders.AsNoTracking()
            .Select(o => new
            {
                o.Id,
                o.Status,
                o.TotalAmount,
                o.OrderTime,
                RestaurantName = o.Restaurant!.Name
            })
            .ToListAsync();

        var delivered = orders.Where(o => o.Status == OrderStatus.Delivered).ToList();
        var active = orders.Where(o => Active.Contains(o.Status)).ToList();

        // Average delivery time = DeliveredTime - OrderTime for delivered orders
        var orderTimes = orders.ToDictionary(o => o.Id, o => o.OrderTime);
        var deliveredTimes = await _db.Deliveries.AsNoTracking()
            .Where(d => d.DeliveredTime != null)
            .Select(d => new { d.OrderId, d.DeliveredTime })
            .ToListAsync();

        var minutes = deliveredTimes
            .Where(d => orderTimes.ContainsKey(d.OrderId))
            .Select(d => (d.DeliveredTime!.Value - orderTimes[d.OrderId]).TotalMinutes)
            .ToList();
        double? avg = minutes.Count > 0 ? Math.Round(minutes.Average(), 1) : null;

        // Orders per hour, grouped in the admin's local time
        var byHour = orders
            .GroupBy(o => o.OrderTime.AddMinutes(tzOffsetMinutes).Hour)
            .ToDictionary(g => g.Key, g => g.Count());
        var hours = Enumerable.Range(0, 24)
            .Select(h => new HourCountDto(h, byHour.GetValueOrDefault(h)))
            .ToList();

        // Delay risk
        var risky = active
            .Select(o => new
            {
                o,
                Elapsed = (int)(now - o.OrderTime).TotalMinutes
            })
            .Where(x => x.Elapsed >= AtRiskMinutes)
            .OrderByDescending(x => x.Elapsed)
            .Select(x => new DelayedOrderDto(
                x.o.Id, x.o.RestaurantName, x.o.Status.ToString(), x.Elapsed,
                x.Elapsed >= DelayedMinutes ? "Delayed" : "AtRisk"))
            .ToList();

        return new AdminStatsDto(
            TotalOrders: orders.Count,
            ActiveOrders: active.Count,
            DeliveredOrders: delivered.Count,
            CancelledOrders: orders.Count(o => o.Status is OrderStatus.Cancelled or OrderStatus.Rejected),
            Revenue: delivered.Sum(o => o.TotalAmount),
            AvgDeliveryMinutes: avg,
            DelayedCount: risky.Count(r => r.Risk == "Delayed"),
            AtRiskCount: risky.Count(r => r.Risk == "AtRisk"),
            OrdersByHour: hours,
            DelayedOrders: risky);
    }
}