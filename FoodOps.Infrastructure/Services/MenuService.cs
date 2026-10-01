using FoodOps.Application.Common;
using FoodOps.Application.DTOs.Menu;
using FoodOps.Application.Interfaces;
using FoodOps.Domain.Entities;
using FoodOps.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace FoodOps.Infrastructure.Services;

public class MenuService : IMenuService
{
    private readonly AppDbContext _db;
    public MenuService(AppDbContext db) => _db = db;

    public async Task<List<MenuItemDto>> GetByRestaurantAsync(int restaurantId)
    {
        return await _db.MenuItems.AsNoTracking()
            .Where(m => m.RestaurantId == restaurantId)
            .OrderBy(m => m.Category).ThenBy(m => m.Name)
            .Select(m => new MenuItemDto(m.Id, m.RestaurantId, m.Name, m.Price, m.Category, m.IsAvailable))
            .ToListAsync();
    }

    public async Task<MenuItemDto> CreateAsync(CreateMenuItemDto dto, Guid userId, bool isAdmin)
    {
        await EnsureOwnerAsync(dto.RestaurantId, userId, isAdmin);

        var item = new MenuItem
        {
            RestaurantId = dto.RestaurantId,
            Name = dto.Name,
            Price = dto.Price,
            Category = dto.Category,
            IsAvailable = true
        };
        _db.MenuItems.Add(item);
        await _db.SaveChangesAsync();
        return ToDto(item);
    }

    public async Task<MenuItemDto> UpdateAsync(int id, UpdateMenuItemDto dto, Guid userId, bool isAdmin)
    {
        var item = await _db.MenuItems.FindAsync(id) ?? throw new NotFoundException("Menu item not found.");
        await EnsureOwnerAsync(item.RestaurantId, userId, isAdmin);

        item.Name = dto.Name;
        item.Price = dto.Price;
        item.Category = dto.Category;
        item.IsAvailable = dto.IsAvailable;
        await _db.SaveChangesAsync();
        return ToDto(item);
    }

    public async Task DeleteAsync(int id, Guid userId, bool isAdmin)
    {
        var item = await _db.MenuItems.FindAsync(id) ?? throw new NotFoundException("Menu item not found.");
        await EnsureOwnerAsync(item.RestaurantId, userId, isAdmin);

        // Past orders reference this item, so hide it instead of deleting.
        if (await _db.OrderItems.AnyAsync(oi => oi.MenuItemId == id))
            item.IsAvailable = false;
        else
            _db.MenuItems.Remove(item);

        await _db.SaveChangesAsync();
    }

    private async Task EnsureOwnerAsync(int restaurantId, Guid userId, bool isAdmin)
    {
        var r = await _db.Restaurants.AsNoTracking().FirstOrDefaultAsync(x => x.Id == restaurantId)
            ?? throw new NotFoundException("Restaurant not found.");
        if (!isAdmin && r.OwnerId != userId) throw new ForbiddenException("Not your restaurant.");
    }

    private static MenuItemDto ToDto(MenuItem m) =>
        new(m.Id, m.RestaurantId, m.Name, m.Price, m.Category, m.IsAvailable);
}