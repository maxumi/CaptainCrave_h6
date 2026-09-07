using Api.DTOs.Auth;

namespace Api.Services;

// Definerer forretningslogik for registrering og login.
public interface IAuthService
{
    // Registrerer en ny bruger og returnerer brugeroplysninger samt JWT-token.
    Task<AuthResponseDto> RegisterAsync(RegisterRequestDto dto);

    // Logger en eksisterende bruger ind og returnerer brugeroplysninger samt JWT-token.
    Task<AuthResponseDto> LoginAsync(LoginRequestDto dto);
}
