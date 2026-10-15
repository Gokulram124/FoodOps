using FoodOps.Domain.Enums;

namespace FoodOps.Domain.Entities;

public class Coupon
{
    public int Id { get; set; }
    public string Code { get; set; } = string.Empty;          // always stored UPPERCASE
    public DiscountType Type { get; set; }
    public decimal Value { get; set; }                        // 20 = 20% or Rs.20, depending on Type
    public decimal? MaxDiscount { get; set; }                 // cap for Percent coupons
    public decimal MinOrderAmount { get; set; }
    public DateTime ValidFromUtc { get; set; }
    public DateTime ValidToUtc { get; set; }
    public Guid? AssignedUserId { get; set; }                 // null = anyone can use
    public int? UsageLimit { get; set; }                      // null = unlimited total uses
    public int UsedCount { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedOn { get; set; } = DateTime.UtcNow;
}