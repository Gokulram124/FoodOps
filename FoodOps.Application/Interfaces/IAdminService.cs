using FoodOps.Application.DTOs.Admin;
using FoodOps.Application.DTOs.Restaurants;

namespace FoodOps.Application.Interfaces;

public interface IAdminService
{
    Task<AdminStatsDto> GetStatsAsync(int tzOffsetMinutes);
    Task<OwnerSummaryDto> CreateOwnerAsync(CreateOwnerDto dto);
    Task<IReadOnlyList<OwnerSummaryDto>> GetOwnersAsync();
    Task<IReadOnlyList<AdminRestaurantDto>> GetRestaurantsAsync(int tzOffsetMinutes);
    Task<RestaurantDto> CreateRestaurantAsync(AdminCreateRestaurantDto dto);
}