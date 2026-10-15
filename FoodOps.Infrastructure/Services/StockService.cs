using FoodOps.Application.Interfaces;
using FoodOps.Application.Notifications;
using FoodOps.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace FoodOps.Infrastructure.Services;

public class StockService : IStockService
{
    private readonly AppDbContext _db;
    private readonly IOrderNotifier _notifier;
    private readonly ILogger<StockService> _logger;

    public StockService(AppDbContext db, IOrderNotifier notifier, ILogger<StockService> logger)
    {
        _db = db;
        _notifier = notifier;
        _logger = logger;
    }

    public async Task ReserveAsync(IReadOnlyDictionary<int, int> quantities)
    {
        // fixed order (by id) so two orders with the same items can't deadlock each other
        foreach (var (id, qty) in quantities.OrderBy(k => k.Key))
        {
            var rows = await _db.MenuItems
                .Where(m => m.Id == id && m.Stock != null && m.Stock >= qty)
                .ExecuteUpdateAsync(s => s.SetProperty(m => m.Stock, m => m.Stock - qty));

            if (rows == 1) continue;

            var stock = await _db.MenuItems.AsNoTracking()
                .Where(m => m.Id == id).Select(m => m.Stock).FirstAsync();

            if (stock is not null)
                throw new InvalidOperationException(
                    stock == 0 ? "An item in your order is sold out."
                               : $"Only {stock} left for an item in your order.");
        }
    }

    public async Task<IReadOnlyList<int>> ReleaseAsync(int orderId)
    {
        var lines = await _db.OrderItems.AsNoTracking()
            .Where(i => i.OrderId == orderId)
            .Select(i => new { i.MenuItemId, i.Quantity })
            .ToListAsync();

        foreach (var l in lines.OrderBy(x => x.MenuItemId))
        {
            var id = l.MenuItemId;
            var qty = l.Quantity;
            await _db.MenuItems
                .Where(m => m.Id == id && m.Stock != null)   // unlimited items stay unlimited
                .ExecuteUpdateAsync(s => s.SetProperty(m => m.Stock, m => m.Stock + qty));
        }
        return lines.Select(l => l.MenuItemId).ToList();
    }

    public async Task PublishAsync(int restaurantId, IEnumerable<int> menuItemIds)
    {
        try
        {
            var ids = menuItemIds.Distinct().ToList();
            var levels = await _db.MenuItems.AsNoTracking()
                .Where(m => ids.Contains(m.Id))
                .Select(m => new StockLevel(m.Id, m.Stock))
                .ToListAsync();

            if (levels.Count > 0)
                await _notifier.StockChangedAsync(restaurantId, levels);
        }
        catch (Exception ex)
        {
            // a failed push must never fail the order or the stock update
            _logger.LogWarning(ex, "Stock push failed for restaurant {RestaurantId}", restaurantId);
        }
    }
}