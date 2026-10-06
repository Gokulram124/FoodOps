using FoodOps.Application.Common;
using FoodOps.Application.DTOs.Restaurants;
using FoodOps.Application.Interfaces;
using FoodOps.Domain.Entities;
using FoodOps.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using System.Text;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;
using System.Text.Json;

namespace FoodOps.Infrastructure.Services;

public class RestaurantService : IRestaurantService
{
    private readonly AppDbContext _db;
    private readonly IMemoryCache _memory;
    private readonly IDistributedCache _distributed;
    private const string CitiesKey = "restaurants:cities";

    public RestaurantService(AppDbContext db, IMemoryCache memory, IDistributedCache distributed)
    {
        _db = db;
        _memory = memory;
        _distributed = distributed;
    }
    private static readonly HashSet<string> Fields =
    new(StringComparer.OrdinalIgnoreCase) { "Name", "City", "Rating", "IsOpen" };

    public async Task<PagedResult<RestaurantDto>> GetAllAsync(RestaurantQuery q)
    {
        var page = Math.Max(q.Page, 1);
        var pageSize = Math.Clamp(q.PageSize, 1, 50);

        var query = _db.Restaurants.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(q.Search))
            query = query.Where(r => r.Name.Contains(q.Search));
        if (!string.IsNullOrWhiteSpace(q.City))
            query = query.Where(r => r.City == q.City);
        if (q.IsOpen.HasValue)
            query = query.Where(r => r.IsOpen == q.IsOpen.Value);
        if (q.MinRating.HasValue)
            query = query.Where(r => r.Rating >= q.MinRating.Value);

        query = query
            .ApplyFilter(q.Filter, Fields)
            .ApplySort(string.IsNullOrWhiteSpace(q.Sort) ? "name" : q.Sort, Fields);

        var total = await query.CountAsync();
        var items = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(r => new RestaurantDto(r.Id, r.Name, r.City, r.Rating, r.IsOpen))
            .ToListAsync();

        return new PagedResult<RestaurantDto>(items, total, page, pageSize);
    }

    public async Task<KeysetResult<RestaurantDto>> GetKeysetAsync(string? cursor, int pageSize)
    {
        pageSize = Math.Clamp(pageSize, 1, 50);
        var query = _db.Restaurants.AsNoTracking().AsQueryable();

        // cursor = base64("name|id") of the LAST item the client already has
        if (!string.IsNullOrEmpty(cursor))
        {
            var text = Encoding.UTF8.GetString(Convert.FromBase64String(cursor));
            var sep = text.LastIndexOf('|');
            var lastName = text[..sep];
            var lastId = int.Parse(text[(sep + 1)..]);

            query = query.Where(r => r.Name.CompareTo(lastName) > 0
                                  || (r.Name == lastName && r.Id > lastId));
        }

        // fetch one extra row to know if there is a next page
        var rows = await query
            .OrderBy(r => r.Name).ThenBy(r => r.Id)
            .Take(pageSize + 1)
            .Select(r => new RestaurantDto(r.Id, r.Name, r.City, r.Rating, r.IsOpen))
            .ToListAsync();

        var hasMore = rows.Count > pageSize;
        var items = rows.Take(pageSize).ToList();

        string? next = null;
        if (hasMore)
        {
            var last = items[^1];
            next = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{last.Name}|{last.Id}"));
        }
        return new KeysetResult<RestaurantDto>(items, next, hasMore);
    }

    public async Task<RestaurantDto> GetByIdAsync(int id)
    {
        var key = $"restaurant:{id}";
        if (_memory.TryGetValue(key, out RestaurantDto? cached) && cached is not null)
            return cached;                                   // cache hit

        var r = await _db.Restaurants.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id)
            ?? throw new NotFoundException("Restaurant not found.");

        var dto = ToDto(r);
        _memory.Set(key, dto, new MemoryCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(2)
        });
        return dto;
    }
    public async Task<IReadOnlyList<string>> GetCitiesAsync()
    {
        var json = await _distributed.GetStringAsync(CitiesKey);
        if (json is not null)
            return JsonSerializer.Deserialize<List<string>>(json)!;   // hit

        var cities = await _db.Restaurants.AsNoTracking()
            .Select(r => r.City).Distinct().OrderBy(c => c).ToListAsync();

        await _distributed.SetStringAsync(CitiesKey, JsonSerializer.Serialize(cities),
            new DistributedCacheEntryOptions { AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(10) });
        return cities;
    }
    public async Task<RestaurantDto> CreateAsync(CreateRestaurantDto dto, Guid userId)
    {
        var r = new Restaurant { Name = dto.Name, City = dto.City, OwnerId = userId, IsOpen = true };
        _db.Restaurants.Add(r);
        await _db.SaveChangesAsync();
        await _distributed.RemoveAsync(CitiesKey);
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
        _memory.Remove($"restaurant:{id}");
        await _distributed.RemoveAsync(CitiesKey);
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
        await _distributed.RemoveAsync(CitiesKey);
    }

    private static RestaurantDto ToDto(Restaurant r) => new(r.Id, r.Name, r.City, r.Rating, r.IsOpen);
}