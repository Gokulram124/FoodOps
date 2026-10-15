using FoodOps.Domain.Enums;

namespace FoodOps.Application.DTOs.Admin;

public record CreateCouponDto(
    string Code, DiscountType Type, decimal Value, decimal? MaxDiscount,
    decimal MinOrderAmount, DateTime ValidFromUtc, DateTime ValidToUtc,
    Guid? AssignedUserId, int? UsageLimit);

public record CouponDto(
    int Id, string Code, DiscountType Type, decimal Value, decimal? MaxDiscount,
    decimal MinOrderAmount, DateTime ValidFromUtc, DateTime ValidToUtc,
    Guid? AssignedUserId, string? AssignedUserName,
    int? UsageLimit, int UsedCount, bool IsActive, string Status);

public record SetCouponActiveDto(bool IsActive);

public record CustomerOptionDto(Guid Id, string FullName, string Email);