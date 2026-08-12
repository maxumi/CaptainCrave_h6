using Api.DTOs.Auth;

namespace Api.Services;

// Defines the registration and login operations
public interface IAuthService
{
    Task<AuthResponseDto> RegisterAsync(RegisterRequestDto dto);
    Task<AuthResponseDto> LoginAsync(LoginRequestDto dto);
}
