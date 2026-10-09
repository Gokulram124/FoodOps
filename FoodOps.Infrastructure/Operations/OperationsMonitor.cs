using FoodOps.Application.DelayRules;
using FoodOps.Application.Operations;
using FoodOps.Domain.Enums;
using FoodOps.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace FoodOps.Infrastructure.Operations;

public class OperationsMonitor : IOperationsMonitor
{
    private static readonly OrderStatus[] Active =
    {
        OrderStatus.Placed, OrderStatus.Accepted, OrderStatus.Preparing,
        OrderStatus.ReadyForPickup, OrderStatus.PickedUp
    };
    private static readonly OrderStatus[] Kitchen =
    {
        OrderStatus.Placed, OrderStatus.Accepted, OrderStatus.Preparing
    };

    private readonly AppDbContext _db;
    private readonly IDelayAnalyzer _delays;
    private readonly IEnumerable<IOpsRule> _rules;
    private readonly TimeProvider _clock;

    public OperationsMonitor(AppDbContext db, IDelayAnalyzer delays, IEnumerable<IOpsRule> rules, TimeProvider clock)
    {
        _db = db;
        _delays = delays;
        _rules = rules;
        _clock = clock;
    }

    public async Task<OpsReport> ScanAsync()
    {
        // Only active orders are loaded (filtered in SQL)
        var active = await _db.Orders.AsNoTracking()
            .Where(o => Active.Contains(o.Status))
            .Select(o => new
            {
                o.Id,
                o.RestaurantId,
                RestaurantName = o.Restaurant!.Name,
                o.Status,
                o.OrderTime,
                o.RiderId
            })
            .ToListAsync();

        var onlineRiderIds = await _db.Riders.AsNoTracking()
            .Where(r => r.IsOnline)
            .Select(r => r.Id)
            .ToListAsync();

        var busyRiderIds = active
            .Where(o => o.RiderId != null)
            .Select(o => o.RiderId!.Value)
            .Distinct()
            .ToList();

        var snapshot = new OpsSnapshot(
            Restaurants: active
                .Where(o => Kitchen.Contains(o.Status))
                .GroupBy(o => new { o.RestaurantId, o.RestaurantName })
                .Select(g => new RestaurantLoad(g.Key.RestaurantId, g.Key.RestaurantName, g.Count()))
                .ToList(),
            WaitingForRider: active.Count(o => o.Status == OrderStatus.ReadyForPickup && o.RiderId == null),
            UpcomingPickups: active.Count(o => o.Status == OrderStatus.Preparing),
            OnlineRiders: onlineRiderIds.Count,
            BusyRiders: onlineRiderIds.Intersect(busyRiderIds).Count());

        var delays = _delays.Analyze(active.Select(o =>
            new ActiveOrder(o.Id, o.RestaurantName, o.Status, o.OrderTime, o.RiderId)));

        var alerts = _rules
            .SelectMany(r => r.Evaluate(snapshot))
            .OrderByDescending(a => a.Severity)
            .ToList();

        return new OpsReport(_clock.GetUtcNow().UtcDateTime, delays, alerts);
    }
}