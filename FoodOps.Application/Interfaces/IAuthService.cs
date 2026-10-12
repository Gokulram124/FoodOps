using FoodOps.Application.DTOs.Auth;

namespace FoodOps.Application.Interfaces;

public interface IAuthService
{
    Task<AuthResponse> RegisterAsync(RegisterDto dto);
    Task<AuthResponse> LoginAsync(LoginDto dto);
    Task<AuthResponse> RefreshAsync(string refreshToken);
    Task LogoutAsync(string refreshToken);
}