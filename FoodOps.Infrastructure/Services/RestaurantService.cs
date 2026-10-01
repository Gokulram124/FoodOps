using FoodOps.Application.Common;
using FoodOps.Application.DTOs.Restaurants;
using FoodOps.Application.Interfaces;
using FoodOps.Domain.Entities;
using FoodOps.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace FoodOps.Infrastructure.Services;

public class RestaurantService : IRestaurantService
{
    private readonly AppDbContext _db;
    public RestaurantService(AppDbContext db) => _db = db;

    public async Task<PagedResult<RestaurantDto>> GetAllAsync(string? search, string? city, int page, int pageSize)
    {
        page = Math.Max(page, 1);
        pageSize = Math.Clamp(pageSize, 1, 50);

        var query = _db.Restaurants.AsNoTracking().AsQueryable();
        if (!string.IsNullOrWhiteSpace(search))
            query = query.Where(r => r.Name.Contains(search));
        if (!string.IsNullOrWhiteSpace(city))
            query = query.Where(r => r.City == city);

        var total = await query.CountAsync();
        var items = await query
            .OrderBy(r => r.Name)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(r => new RestaurantDto(r.Id, r.Name, r.City, r.Rating, r.IsOpen))
            .ToListAsync();

        return new PagedResult<RestaurantDto>(items, total, page, pageSize);
    }

    public async Task<RestaurantDto> GetByIdAsync(int id)
    {
        var r = await _db.Restaurants.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id)
            ?? throw new NotFoundException("Restaurant not found.");
        return ToDto(r);
    }

    public async Task<RestaurantDto> CreateAsync(CreateRestaurantDto dto, Guid userId)
    {
        var r = new Restaurant { Name = dto.Name, City = dto.City, OwnerId = userId, IsOpen = true };
        _db.Restaurants.Add(r);
        await _db.SaveChangesAsync();
        return ToDto(r);
    }

    public async Task<RestaurantDto> UpdateAsync(int id, UpdateRestaurantDto dto, Guid userId, bool isAdmin)
    {
        var r = await _db.Restaurants.FindAsync(id) ?? throw new NotFoundException("Restaurant not found.");
        if (!isAdmin && r.OwnerId != userId) throw new ForbiddenException("Not your restaurant.");

        r.Name = dto.Name;
        r.City = dto.City;
        r.IsOpen = dto.IsOpen;
        await _db.SaveChangesAsync();
        return ToDto(r);
    }

    public async Task DeleteAsync(int id, Guid userId, bool isAdmin)
    {
        var r = await _db.Restaurants.FindAsync(id) ?? throw new NotFoundException("Restaurant not found.");
        if (!isAdmin && r.OwnerId != userId) throw new ForbiddenException("Not your restaurant.");

        if (await _db.Orders.AnyAsync(o => o.RestaurantId == id))
            throw new InvalidOperationException("Restaurant has orders. Mark it closed instead of deleting.");

        _db.Restaurants.Remove(r);
        await _db.SaveChangesAsync();
    }

    private static RestaurantDto ToDto(Restaurant r) => new(r.Id, r.Name, r.City, r.Rating, r.IsOpen);
}