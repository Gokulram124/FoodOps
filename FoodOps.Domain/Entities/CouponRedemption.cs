namespace FoodOps.Domain.Entities;

public class CouponRedemption
{
    public int Id { get; set; }
    public int CouponId { get; set; }
    public Coupon? Coupon { get; set; }
    public Guid UserId { get; set; }
    public int OrderId { get; set; }
    public DateTime RedeemedOn { get; set; } = DateTime.UtcNow;
}