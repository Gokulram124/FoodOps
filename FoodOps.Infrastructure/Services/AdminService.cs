using FoodOps.Application.DelayRules;
using FoodOps.Application.DTOs.Admin;
using FoodOps.Application.Interfaces;
using FoodOps.Domain.Enums;
using FoodOps.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using FoodOps.Application.Common;
using Microsoft.AspNetCore.Identity;
using FoodOps.Application.DTOs.Restaurants;

namespace FoodOps.Infrastructure.Services;

public class AdminService : IAdminService
{
    private readonly AppDbContext _db;
    private readonly IDelayAnalyzer _analyzer;
    private readonly UserManager<ApplicationUser> _users;
    private readonly IRestaurantService _restaurants;

    public AdminService(AppDbContext db, IDelayAnalyzer analyzer,
        UserManager<ApplicationUser> users, IRestaurantService restaurants)
    {
        _db = db;
        _analyzer = analyzer;
        _users = users;
        _restaurants = restaurants;
    }



    private static readonly OrderStatus[] Active =
    {
        OrderStatus.Placed, OrderStatus.Accepted, OrderStatus.Preparing,
        OrderStatus.ReadyForPickup, OrderStatus.PickedUp
    };

    public async Task<AdminStatsDto> GetStatsAsync(int tzOffsetMinutes)
    {
        var now = DateTime.UtcNow;

        // Fine for a portfolio-size dataset. At scale you would push the grouping into SQL.
        var orders = await _db.Orders.AsNoTracking()
        .Select(o => new
        {
            o.Id,
            o.Status,
            o.TotalAmount,
            o.OrderTime,
            o.RiderId,
            RestaurantName = o.Restaurant!.Name
        })
        .ToListAsync();

        var delivered = orders.Where(o => o.Status == OrderStatus.Delivered).ToList();
        var active = orders.Where(o => Active.Contains(o.Status)).ToList();

        // Average delivery time = DeliveredTime - OrderTime for delivered orders
        var orderTimes = orders.ToDictionary(o => o.Id, o => o.OrderTime);
        var deliveredTimes = await _db.Deliveries.AsNoTracking()
            .Where(d => d.DeliveredTime != null)
            .Select(d => new { d.OrderId, d.DeliveredTime })
            .ToListAsync();

        var minutes = deliveredTimes
            .Where(d => orderTimes.ContainsKey(d.OrderId))
            .Select(d => (d.DeliveredTime!.Value - orderTimes[d.OrderId]).TotalMinutes)
            .ToList();
        double? avg = minutes.Count > 0 ? Math.Round(minutes.Average(), 1) : null;

        // Orders per hour, grouped in the admin's local time
        var byHour = orders
            .GroupBy(o => o.OrderTime.AddMinutes(tzOffsetMinutes).Hour)
            .ToDictionary(g => g.Key, g => g.Count());
        var hours = Enumerable.Range(0, 24)
            .Select(h => new HourCountDto(h, byHour.GetValueOrDefault(h)))
            .ToList();


        // Delay risk: every rule looks at every active order, the highest risk wins
        var findings = _analyzer.Analyze(active.Select(o =>
            new ActiveOrder(o.Id, o.RestaurantName, o.Status, o.OrderTime, o.RiderId)));

        var risky = findings
            .Select(f => new DelayedOrderDto(
                f.OrderId, f.RestaurantName, f.Status.ToString(), f.ElapsedMinutes,
                f.Level.ToString(), string.Join("; ", f.Reasons)))
            .ToList();
        return new AdminStatsDto(
            TotalOrders: orders.Count,
            ActiveOrders: active.Count,
            DeliveredOrders: delivered.Count,
            CancelledOrders: orders.Count(o => o.Status is OrderStatus.Cancelled or OrderStatus.Rejected),
            Revenue: delivered.Sum(o => o.TotalAmount),
            AvgDeliveryMinutes: avg,
            DelayedCount: risky.Count(r => r.Risk == "Delayed"),
            AtRiskCount: risky.Count(r => r.Risk == "AtRisk"),
            OrdersByHour: hours,
            DelayedOrders: risky);
    }

    public async Task<OwnerSummaryDto> CreateOwnerAsync(CreateOwnerDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.FullName) || string.IsNullOrWhiteSpace(dto.Email)
            || string.IsNullOrWhiteSpace(dto.Password) || !dto.Email.Contains('@'))
            throw new InvalidOperationException("Name, a valid email and a password are required.");

        var email = dto.Email.Trim().ToLowerInvariant();
        if (await _users.FindByEmailAsync(email) is not null)
            throw new InvalidOperationException("An account with this email already exists.");

        var user = new ApplicationUser
        {
            Id = Guid.NewGuid(),
            UserName = email,
            Email = email,
            FullName = dto.FullName.Trim(),
            EmailConfirmed = true
        };

        var result = await _users.CreateAsync(user, dto.Password);
        if (!result.Succeeded)
            throw new InvalidOperationException(string.Join(" ", result.Errors.Select(e => e.Description)));

        await _users.AddToRoleAsync(user, Roles.RestaurantOwner);
        return new OwnerSummaryDto(user.Id, user.FullName, email, 0);
    }

    public async Task<IReadOnlyList<OwnerSummaryDto>> GetOwnersAsync()
    {
        var owners = await _users.GetUsersInRoleAsync(Roles.RestaurantOwner);

        // one grouped query for all counts, not one query per owner (avoids N+1)
        var counts = await _db.Restaurants.AsNoTracking()
            .GroupBy(r => r.OwnerId)
            .Select(g => new { OwnerId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.OwnerId, x => x.Count);

        return owners
            .OrderBy(o => o.FullName)
            .Select(o => new OwnerSummaryDto(o.Id, o.FullName, o.Email ?? "", counts.GetValueOrDefault(o.Id)))
            .ToList();
    }

    public async Task<IReadOnlyList<AdminRestaurantDto>> GetRestaurantsAsync(int tzOffsetMinutes)
    {
        // "today" in the admin's local time, converted back to UTC (the DB stores UTC)
        var startUtc = DateTime.UtcNow.AddMinutes(tzOffsetMinutes).Date.AddMinutes(-tzOffsetMinutes);

        var restaurants = await _db.Restaurants.AsNoTracking()
            .OrderBy(r => r.Name)
            .Select(r => new { r.Id, r.Name, r.City, r.IsOpen, r.OwnerId })
            .ToListAsync();

        // orders placed today, plus any order still open from earlier (so "active" is never undercounted)
        var orders = await _db.Orders.AsNoTracking()
            .Where(o => o.OrderTime >= startUtc
                     || (o.Status != OrderStatus.Delivered
                         && o.Status != OrderStatus.Rejected
                         && o.Status != OrderStatus.Cancelled))
            .Select(o => new { o.RestaurantId, o.Status, o.TotalAmount, o.OrderTime })
            .ToListAsync();
        var byRestaurant = orders.ToLookup(o => o.RestaurantId);

        var ownerIds = restaurants.Select(r => r.OwnerId).Distinct().ToList();
        var ownerNames = await _users.Users
            .Where(u => ownerIds.Contains(u.Id))
            .ToDictionaryAsync(u => u.Id, u => u.FullName);

        return restaurants.Select(r =>
        {
            var list = byRestaurant[r.Id].ToList();
            var today = list.Where(o => o.OrderTime >= startUtc).ToList();
            return new AdminRestaurantDto(
                r.Id, r.Name, r.City, r.IsOpen, r.OwnerId,
                ownerNames.GetValueOrDefault(r.OwnerId, "(unknown)"),
                OrdersToday: today.Count,
                RevenueToday: today.Where(o => o.Status == OrderStatus.Delivered).Sum(o => o.TotalAmount),
                ActiveNow: list.Count(o => Active.Contains(o.Status)));
        }).ToList();
    }

    public async Task<RestaurantDto> CreateRestaurantAsync(AdminCreateRestaurantDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Name) || string.IsNullOrWhiteSpace(dto.City))
            throw new InvalidOperationException("Name and city are required.");

        var owner = await _users.FindByIdAsync(dto.OwnerId.ToString());
        if (owner is null || !await _users.IsInRoleAsync(owner, Roles.RestaurantOwner))
            throw new NotFoundException("Owner not found.");

        // reuse the existing logic (also clears the cities cache)
        return await _restaurants.CreateAsync(new CreateRestaurantDto(dto.Name.Trim(), dto.City.Trim()), owner.Id);
    }
}