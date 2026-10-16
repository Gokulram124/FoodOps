using FoodOps.Application.Coupons;
using FoodOps.Application.Interfaces;
using FoodOps.Domain.Entities;
using FoodOps.Domain.Enums;
using FoodOps.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace FoodOps.Infrastructure.Services;

public class CouponRedeemer : ICouponRedeemer
{
    private readonly AppDbContext _db;
    private readonly Dictionary<DiscountType, IDiscountStrategy> _strategies;
    private readonly TimeProvider _time;

    public CouponRedeemer(AppDbContext db, IEnumerable<IDiscountStrategy> strategies, TimeProvider time)
    {
        _db = db;
        _time = time;
        _strategies = strategies.ToDictionary(s => s.Type);
    }

    public async Task<CouponApplication> PrepareAsync(string code, Guid userId, decimal subtotal)
    {
        var normalized = (code ?? string.Empty).Trim().ToUpperInvariant();
        var coupon = await _db.Coupons.AsNoTracking().FirstOrDefaultAsync(c => c.Code == normalized);

        // one message for "no such code", "disabled" and "somebody else's coupon": codes can't be probed
        if (coupon is null || !coupon.IsActive || (coupon.AssignedUserId is { } owner && owner != userId))
            throw new InvalidOperationException("This coupon code is not valid.");

        var now = _time.GetUtcNow().UtcDateTime;
        if (now < coupon.ValidFromUtc)
            throw new InvalidOperationException("This coupon is not active yet.");
        if (now > coupon.ValidToUtc)
            throw new InvalidOperationException("This coupon has expired.");
        if (subtotal < coupon.MinOrderAmount)
            throw new InvalidOperationException($"Minimum order for this coupon is ₹{coupon.MinOrderAmount:0.##}.");
        if (coupon.UsageLimit is { } limit && coupon.UsedCount >= limit)
            throw new InvalidOperationException("This coupon has been fully used.");

        // friendly early message; the unique index in RedeemAsync is the real guarantee
        if (await _db.CouponRedemptions.AnyAsync(r => r.CouponId == coupon.Id && r.UserId == userId))
            throw new InvalidOperationException("You have already used this coupon.");

        var discount = _strategies[coupon.Type].Calculate(coupon, subtotal);
        if (discount <= 0)
            throw new InvalidOperationException("This coupon gives no discount on this order.");

        return new CouponApplication(coupon.Id, coupon.Code, discount);
    }

    public async Task<CouponQuote> QuoteAsync(string code, Guid userId, int restaurantId,
        IReadOnlyDictionary<int, int> items)
    {
        var ids = items.Keys.ToList();
        var prices = await _db.MenuItems.AsNoTracking()
            .Where(m => m.RestaurantId == restaurantId && ids.Contains(m.Id))
            .ToDictionaryAsync(m => m.Id, m => m.Price);

        if (prices.Count != ids.Count)
            throw new InvalidOperationException("One or more items do not belong to this restaurant.");

        var subtotal = items.Sum(kv => prices[kv.Key] * kv.Value);
        var applied = await PrepareAsync(code, userId, subtotal);
        return new CouponQuote(applied.Code, subtotal, applied.Discount, subtotal - applied.Discount);
    }

    public async Task RedeemAsync(CouponApplication applied, Guid userId, int orderId)
    {
        // 1) total limit: one atomic statement, so two buyers can't both take the last use
        var rows = await _db.Coupons
            .Where(c => c.Id == applied.CouponId && c.IsActive
                        && (c.UsageLimit == null || c.UsedCount < c.UsageLimit))
            .ExecuteUpdateAsync(s => s.SetProperty(c => c.UsedCount, c => c.UsedCount + 1));

        if (rows == 0)
            throw new InvalidOperationException("This coupon has been fully used.");

        // 2) one use per customer: the unique index (CouponId, UserId) decides, not this code
        _db.CouponRedemptions.Add(new CouponRedemption
        {
            CouponId = applied.CouponId,
            UserId = userId,
            OrderId = orderId,
            RedeemedOn = _time.GetUtcNow().UtcDateTime
        });

        try
        {
            await _db.SaveChangesAsync();
        }
        catch (DbUpdateException)
        {
            if (await _db.CouponRedemptions.AsNoTracking()
                    .AnyAsync(r => r.CouponId == applied.CouponId && r.UserId == userId))
                throw new InvalidOperationException("You have already used this coupon.");

            throw;
        }
    }

    public async Task ReleaseAsync(int orderId)
    {
        var redemption = await _db.CouponRedemptions.AsNoTracking()
            .Where(r => r.OrderId == orderId)
            .Select(r => new { r.Id, r.CouponId })
            .FirstOrDefaultAsync();
        if (redemption is null) return;

        // only the caller that really deleted the row gives the use back, so a double cancel can't inflate it
        var deleted = await _db.CouponRedemptions.Where(r => r.Id == redemption.Id).ExecuteDeleteAsync();
        if (deleted == 1)
            await _db.Coupons
                .Where(c => c.Id == redemption.CouponId && c.UsedCount > 0)
                .ExecuteUpdateAsync(s => s.SetProperty(c => c.UsedCount, c => c.UsedCount - 1));
    }
}