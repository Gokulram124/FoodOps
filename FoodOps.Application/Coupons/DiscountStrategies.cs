using FoodOps.Domain.Entities;
using FoodOps.Domain.Enums;

namespace FoodOps.Application.Coupons;

public interface IDiscountStrategy
{
    DiscountType Type { get; }
    decimal Calculate(Coupon coupon, decimal subtotal);
}

public class PercentDiscountStrategy : IDiscountStrategy
{
    public DiscountType Type => DiscountType.Percent;

    public decimal Calculate(Coupon c, decimal subtotal)
    {
        var d = Math.Round(subtotal * c.Value / 100m, 2, MidpointRounding.AwayFromZero);
        if (c.MaxDiscount is { } cap) d = Math.Min(d, cap);
        return Math.Min(d, subtotal);          // never more than the bill
    }
}

public class FlatDiscountStrategy : IDiscountStrategy
{
    public DiscountType Type => DiscountType.Flat;

    public decimal Calculate(Coupon c, decimal subtotal) => Math.Min(c.Value, subtotal);
}