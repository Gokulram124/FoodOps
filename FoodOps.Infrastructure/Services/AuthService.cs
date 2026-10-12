using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using FoodOps.Application.Common;
using FoodOps.Application.DTOs.Auth;
using FoodOps.Application.Interfaces;
using FoodOps.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;

namespace FoodOps.Infrastructure.Services;

public class AuthService : IAuthService
{
    private readonly UserManager<ApplicationUser> _users;
    private readonly IConfiguration _config;
    private readonly AppDbContext _db;
    private readonly TimeProvider _time;

    public AuthService(UserManager<ApplicationUser> users, IConfiguration config, AppDbContext db, TimeProvider time)
    {
        _users = users;
        _config = config;
        _db = db;
        _time = time;
    }

    public async Task<AuthResponse> RegisterAsync(RegisterDto dto)
    {
        if (dto.Role == Roles.Admin || !Roles.All.Contains(dto.Role))
            throw new InvalidOperationException("Invalid role.");

        var user = new ApplicationUser
        {
            Id = Guid.NewGuid(),
            UserName = dto.Email,
            Email = dto.Email,
            FullName = dto.FullName
        };

        var result = await _users.CreateAsync(user, dto.Password);
        if (!result.Succeeded)
            throw new InvalidOperationException(string.Join("; ", result.Errors.Select(e => e.Description)));

        await _users.AddToRoleAsync(user, dto.Role);
        return await IssueAsync(user, dto.Role);
    }

    public async Task<AuthResponse> LoginAsync(LoginDto dto)
    {
        var user = await _users.FindByEmailAsync(dto.Email);
        if (user == null || !await _users.CheckPasswordAsync(user, dto.Password))
            throw new UnauthorizedAccessException("Invalid email or password.");

        var role = (await _users.GetRolesAsync(user)).First();
        return await IssueAsync(user, role);
    }

    public async Task<AuthResponse> RefreshAsync(string refreshToken)
    {
        var hash = Hash(refreshToken);
        var stored = await _db.RefreshTokens.FirstOrDefaultAsync(t => t.TokenHash == hash)
            ?? throw new UnauthorizedAccessException("Invalid refresh token.");

        var now = _time.GetUtcNow().UtcDateTime;

        if (stored.RevokedAt != null)
        {
            // an already-used token came back: possible theft, so kill every session of this user
            await _db.RefreshTokens
                .Where(t => t.UserId == stored.UserId && t.RevokedAt == null)
                .ExecuteUpdateAsync(s => s.SetProperty(t => t.RevokedAt, now));
            throw new UnauthorizedAccessException("Refresh token was already used. Please login again.");
        }
        if (stored.ExpiresAt <= now)
            throw new UnauthorizedAccessException("Refresh token expired. Please login again.");

        var user = await _users.FindByIdAsync(stored.UserId.ToString())
            ?? throw new UnauthorizedAccessException("User no longer exists.");
        var role = (await _users.GetRolesAsync(user)).First();

        // rotate: issue a new pair, retire the old token
        var response = await IssueAsync(user, role);
        stored.RevokedAt = now;
        stored.ReplacedByHash = Hash(response.RefreshToken);
        await _db.SaveChangesAsync();
        return response;
    }

    public async Task LogoutAsync(string refreshToken)
    {
        var hash = Hash(refreshToken);
        var stored = await _db.RefreshTokens.FirstOrDefaultAsync(t => t.TokenHash == hash && t.RevokedAt == null);
        if (stored == null) return;
        stored.RevokedAt = _time.GetUtcNow().UtcDateTime;
        await _db.SaveChangesAsync();
    }

    private async Task<AuthResponse> IssueAsync(ApplicationUser user, string role)
    {
        var raw = Convert.ToBase64String(RandomNumberGenerator.GetBytes(64));
        var now = _time.GetUtcNow().UtcDateTime;
        var days = int.Parse(_config["Jwt:RefreshTokenDays"] ?? "7");

        _db.RefreshTokens.Add(new RefreshToken
        {
            UserId = user.Id,
            TokenHash = Hash(raw),
            CreatedAt = now,
            ExpiresAt = now.AddDays(days)
        });
        await _db.SaveChangesAsync();

        return BuildResponse(user, role, raw);
    }

    private static string Hash(string raw) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(raw)));

    private AuthResponse BuildResponse(ApplicationUser user, string role, string refreshToken)
    {
        var expires = _time.GetUtcNow().UtcDateTime.AddMinutes(double.Parse(_config["Jwt:ExpiryMinutes"]!));
        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim(ClaimTypes.Email, user.Email!),
            new Claim(ClaimTypes.Name, user.FullName),
            new Claim(ClaimTypes.Role, role)
        };

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_config["Jwt:Key"]!));
        var token = new JwtSecurityToken(
            issuer: _config["Jwt:Issuer"],
            audience: _config["Jwt:Audience"],
            claims: claims,
            expires: expires,
            signingCredentials: new SigningCredentials(key, SecurityAlgorithms.HmacSha256));

        return new AuthResponse(new JwtSecurityTokenHandler().WriteToken(token),
            user.Email!, user.FullName, role, expires, refreshToken);
    }
}