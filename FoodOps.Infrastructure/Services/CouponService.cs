using System.Text.RegularExpressions;
using FoodOps.Application.Common;
using FoodOps.Application.DTOs.Admin;
using FoodOps.Application.Interfaces;
using FoodOps.Domain.Entities;
using FoodOps.Domain.Enums;
using FoodOps.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace FoodOps.Infrastructure.Services;

public class CouponService : ICouponService
{
    private static readonly Regex CodePattern = new("^[A-Z0-9_-]{4,30}$", RegexOptions.Compiled);

    private readonly AppDbContext _db;
    private readonly UserManager<ApplicationUser> _users;

    public CouponService(AppDbContext db, UserManager<ApplicationUser> users)
    {
        _db = db;
        _users = users;
    }

    public async Task<IReadOnlyList<CouponDto>> GetAllAsync()
    {
        var coupons = await _db.Coupons.AsNoTracking()
            .OrderByDescending(c => c.CreatedOn).ToListAsync();

        var userIds = coupons.Where(c => c.AssignedUserId != null)
            .Select(c => c.AssignedUserId!.Value).Distinct().ToList();
        var names = await _users.Users
            .Where(u => userIds.Contains(u.Id))
            .ToDictionaryAsync(u => u.Id, u => u.FullName);

        var now = DateTime.UtcNow;
        return coupons.Select(c => ToDto(c,
            c.AssignedUserId is { } id ? names.GetValueOrDefault(id) : null, now)).ToList();
    }

    public async Task<CouponDto> CreateAsync(CreateCouponDto dto)
    {
        var code = (dto.Code ?? string.Empty).Trim().ToUpperInvariant();
        if (!CodePattern.IsMatch(code))
            throw new InvalidOperationException("Code must be 4-30 characters: letters, digits, - or _.");
        if (!Enum.IsDefined(dto.Type))
            throw new InvalidOperationException("Unknown discount type.");
        if (dto.Value <= 0)
            throw new InvalidOperationException("Discount value must be above 0.");
        if (dto.Type == DiscountType.Percent && dto.Value > 100)
            throw new InvalidOperationException("A percent discount cannot be more than 100.");
        if (dto.MaxDiscount is <= 0)
            throw new InvalidOperationException("Maximum discount must be above 0.");
        if (dto.MinOrderAmount < 0)
            throw new InvalidOperationException("Minimum order amount cannot be negative.");
        if (dto.UsageLimit is < 1)
            throw new InvalidOperationException("Usage limit must be at least 1.");

        var from = AsUtc(dto.ValidFromUtc);
        var to = AsUtc(dto.ValidToUtc);
        if (to <= from)
            throw new InvalidOperationException("End time must be after the start time.");
        if (to <= DateTime.UtcNow)
            throw new InvalidOperationException("End time is already in the past.");

        string? assignedName = null;
        if (dto.AssignedUserId is { } uid)
        {
            var user = await _users.FindByIdAsync(uid.ToString());
            if (user is null || !await _users.IsInRoleAsync(user, Roles.Customer))
                throw new NotFoundException("The assigned user must be an existing customer.");
            assignedName = user.FullName;
        }

        if (await _db.Coupons.AnyAsync(c => c.Code == code))
            throw new InvalidOperationException("This coupon code already exists.");

        var coupon = new Coupon
        {
            Code = code,
            Type = dto.Type,
            Value = dto.Value,
            // a cap only makes sense for percent coupons
            MaxDiscount = dto.Type == DiscountType.Percent ? dto.MaxDiscount : null,
            MinOrderAmount = dto.MinOrderAmount,
            ValidFromUtc = from,
            ValidToUtc = to,
            AssignedUserId = dto.AssignedUserId,
            UsageLimit = dto.UsageLimit
        };
        _db.Coupons.Add(coupon);

        try
        {
            await _db.SaveChangesAsync();
        }
        catch (DbUpdateException)
        {
            // two admins created the same code at the same moment: the unique index stopped the second one
            if (await _db.Coupons.AsNoTracking().AnyAsync(c => c.Code == code))
                throw new InvalidOperationException("This coupon code already exists.");

            throw;   // some other database problem: don't hide it
        }

        return ToDto(coupon, assignedName, DateTime.UtcNow);
    }

    public async Task<CouponDto> SetActiveAsync(int id, bool isActive)
    {
        var coupon = await _db.Coupons.FirstOrDefaultAsync(c => c.Id == id)
            ?? throw new NotFoundException("Coupon not found.");

        coupon.IsActive = isActive;
        await _db.SaveChangesAsync();

        string? name = null;
        if (coupon.AssignedUserId is { } uid)
            name = (await _users.FindByIdAsync(uid.ToString()))?.FullName;

        return ToDto(coupon, name, DateTime.UtcNow);
    }

    public async Task<IReadOnlyList<CustomerOptionDto>> GetCustomersAsync()
    {
        var customers = await _users.GetUsersInRoleAsync(Roles.Customer);
        return customers
            .OrderBy(u => u.FullName)
            .Select(u => new CustomerOptionDto(u.Id, u.FullName, u.Email ?? string.Empty))
            .ToList();
    }

    private static DateTime AsUtc(DateTime d) =>
        d.Kind == DateTimeKind.Unspecified ? DateTime.SpecifyKind(d, DateTimeKind.Utc) : d.ToUniversalTime();

    private static string StatusOf(Coupon c, DateTime now) =>
        !c.IsActive ? "Disabled"
        : c.UsageLimit is { } limit && c.UsedCount >= limit ? "Used up"
        : now < c.ValidFromUtc ? "Scheduled"
        : now > c.ValidToUtc ? "Expired"
        : "Active";

    private static CouponDto ToDto(Coupon c, string? assignedName, DateTime now) =>
        new(c.Id, c.Code, c.Type, c.Value, c.MaxDiscount, c.MinOrderAmount,
            c.ValidFromUtc, c.ValidToUtc, c.AssignedUserId, assignedName,
            c.UsageLimit, c.UsedCount, c.IsActive, StatusOf(c, now));
}