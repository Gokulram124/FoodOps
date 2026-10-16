namespace FoodOps.Application.Interfaces;

public record CouponApplication(int CouponId, string Code, decimal Discount);
public record CouponQuote(string Code, decimal Subtotal, decimal Discount, decimal Total);

public interface ICouponRedeemer
{
    /// Checks every rule and works out the discount. Changes nothing.
    Task<CouponApplication> PrepareAsync(string code, Guid userId, decimal subtotal);

    /// Same checks for a cart, with the subtotal taken from database prices (for the "Apply" button).
    Task<CouponQuote> QuoteAsync(string code, Guid userId, int restaurantId, IReadOnlyDictionary<int, int> items);

    /// Uses the coupon for an order. Call inside the order's transaction.
    Task RedeemAsync(CouponApplication applied, Guid userId, int orderId);

    /// Gives the coupon back when an order is cancelled/rejected. Safe to call twice.
    Task ReleaseAsync(int orderId);
}