using FoodOps.Application.DTOs.Menu;

namespace FoodOps.Application.Interfaces;

public interface IMenuService
{
    Task<List<MenuItemDto>> GetByRestaurantAsync(int restaurantId);
    Task<MenuItemDto> CreateAsync(CreateMenuItemDto dto, Guid userId, bool isAdmin);
    Task<MenuItemDto> UpdateAsync(int id, UpdateMenuItemDto dto, Guid userId, bool isAdmin);
    Task DeleteAsync(int id, Guid userId, bool isAdmin);
    Task<MenuItemDto> UploadImageAsync(int id, Stream content, string fileName, long length, Guid userId, bool isAdmin);
}