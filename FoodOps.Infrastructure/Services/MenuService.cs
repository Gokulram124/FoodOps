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
    private readonly IFileStorage _files;
    public MenuService(AppDbContext db, IFileStorage files)
    {
        _db = db;
        _files = files;
    }

    public async Task<List<MenuItemDto>> GetByRestaurantAsync(int restaurantId)
    {
        return await _db.MenuItems.AsNoTracking()
            .Where(m => m.RestaurantId == restaurantId)
            .OrderBy(m => m.Category).ThenBy(m => m.Name)
            .Select(m => new MenuItemDto(m.Id, m.RestaurantId, m.Name, m.Price, m.Category, m.IsAvailable, m.ImageUrl))
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
    new(m.Id, m.RestaurantId, m.Name, m.Price, m.Category, m.IsAvailable, m.ImageUrl);
    private const long MaxImageBytes = 2 * 1024 * 1024;   // 2 MB
    private static readonly string[] AllowedExt = { ".jpg", ".jpeg", ".png", ".webp" };

    public async Task<MenuItemDto> UploadImageAsync(int id, Stream content, string fileName, long length, Guid userId, bool isAdmin)
    {
        var item = await _db.MenuItems.FindAsync(id) ?? throw new NotFoundException("Menu item not found.");
        await EnsureOwnerAsync(item.RestaurantId, userId, isAdmin);

        var ext = Path.GetExtension(fileName).ToLowerInvariant();
        if (!AllowedExt.Contains(ext))
            throw new ArgumentException("Only .jpg, .png or .webp images are allowed.");
        if (length <= 0 || length > MaxImageBytes)
            throw new ArgumentException("Image must be smaller than 2 MB.");

        // check the real file header, not only the extension
        var header = new byte[12];
        var read = await content.ReadAsync(header, 0, header.Length);
        if (read < 12 || !LooksLikeImage(header))
            throw new ArgumentException("File is not a valid image.");
        content.Position = 0;

        // never use the user's file name on disk
        var newName = $"{id}-{Guid.NewGuid():N}{ext}";
        var url = await _files.SaveAsync(content, newName, "menu");

        if (!string.IsNullOrEmpty(item.ImageUrl)) _files.Delete(item.ImageUrl);   // remove old image
        item.ImageUrl = url;
        await _db.SaveChangesAsync();
        return ToDto(item);
    }

    private static bool LooksLikeImage(byte[] h) =>
        (h[0] == 0xFF && h[1] == 0xD8 && h[2] == 0xFF) ||                                   // JPEG
        (h[0] == 0x89 && h[1] == 0x50 && h[2] == 0x4E && h[3] == 0x47) ||                   // PNG
        (h[0] == 0x52 && h[1] == 0x49 && h[2] == 0x46 && h[3] == 0x46 &&
         h[8] == 0x57 && h[9] == 0x45 && h[10] == 0x42 && h[11] == 0x50);                   // WEBP
}