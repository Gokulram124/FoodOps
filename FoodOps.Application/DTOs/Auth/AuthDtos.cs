namespace FoodOps.Application.DTOs.Auth;

public record RegisterDto(string FullName, string Email, string Password, string Role);
public record LoginDto(string Email, string Password);
public record AuthResponse(string Token, string Email, string FullName, string Role, DateTime ExpiresAt);