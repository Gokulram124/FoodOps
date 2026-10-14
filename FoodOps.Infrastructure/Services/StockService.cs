using FoodOps.Application.Interfaces;
using FoodOps.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace FoodOps.Infrastructure.Services;

public class StockService : IStockService
{
    private readonly AppDbContext _db;
    public StockService(AppDbContext db) => _db = db;

    public async Task ReserveAsync(IReadOnlyDictionary<int, int> quantities)
    {
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
}