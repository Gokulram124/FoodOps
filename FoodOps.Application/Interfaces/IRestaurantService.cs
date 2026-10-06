using FoodOps.Application.Common;
using FoodOps.Application.DTOs.Restaurants;

namespace FoodOps.Application.Interfaces;

public interface IRestaurantService
{
    Task<PagedResult<RestaurantDto>> GetAllAsync(RestaurantQuery query);
    Task<RestaurantDto> GetByIdAsync(int id);
    Task<RestaurantDto> CreateAsync(CreateRestaurantDto dto, Guid userId);
    Task<RestaurantDto> UpdateAsync(int id, UpdateRestaurantDto dto, Guid userId, bool isAdmin);
    Task DeleteAsync(int id, Guid userId, bool isAdmin);
    Task<KeysetResult<RestaurantDto>> GetKeysetAsync(string? cursor, int pageSize);
    Task<IReadOnlyList<string>> GetCitiesAsync();
}