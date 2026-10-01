using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using FoodOps.Application.Common;
using FoodOps.Application.DTOs.Auth;
using FoodOps.Application.Interfaces;
using FoodOps.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;

namespace FoodOps.Infrastructure.Services;

public class AuthService : IAuthService
{
    private readonly UserManager<ApplicationUser> _users;
    private readonly IConfiguration _config;

    public AuthService(UserManager<ApplicationUser> users, IConfiguration config)
    {
        _users = users;
        _config = config;
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
        return BuildResponse(user, dto.Role);
    }

    public async Task<AuthResponse> LoginAsync(LoginDto dto)
    {
        var user = await _users.FindByEmailAsync(dto.Email);
        if (user == null || !await _users.CheckPasswordAsync(user, dto.Password))
            throw new UnauthorizedAccessException("Invalid email or password.");

        var role = (await _users.GetRolesAsync(user)).First();
        return BuildResponse(user, role);
    }

    private AuthResponse BuildResponse(ApplicationUser user, string role)
    {
        var expires = DateTime.UtcNow.AddMinutes(double.Parse(_config["Jwt:ExpiryMinutes"]!));
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
            user.Email!, user.FullName, role, expires);
    }
}