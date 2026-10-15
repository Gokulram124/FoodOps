using FoodOps.Application.DTOs.Admin;

namespace FoodOps.Application.Interfaces;

public interface ICouponService
{
    Task<IReadOnlyList<CouponDto>> GetAllAsync();
    Task<CouponDto> CreateAsync(CreateCouponDto dto);
    Task<CouponDto> SetActiveAsync(int id, bool isActive);
    Task<IReadOnlyList<CustomerOptionDto>> GetCustomersAsync();
}