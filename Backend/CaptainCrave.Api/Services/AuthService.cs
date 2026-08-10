using Api.DTOs.Auth;
using Api.Mappers;
using Api.Repositories;

namespace Api.Services;

// Handles the business logic for registering and logging in users
public class AuthService(IUserRepository userRepository, ITokenService tokenService) : IAuthService
{
    // Validates the email is not taken, hashes the password, saves the user and returns a JWT
    public async Task<AuthResponseDto> RegisterAsync(RegisterRequestDto dto)
    {
        if (await userRepository.EmailExistsAsync(dto.Email))
            throw new InvalidOperationException("Email is already in use.");

        var user = dto.ToUser();
        user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password);

        var created = await userRepository.CreateAsync(user);
        var token = tokenService.GenerateToken(created);

        return created.ToAuthResponseDto(token);
    }

    // Finds the user by email, verifies the password and returns a JWT
    public async Task<AuthResponseDto> LoginAsync(LoginRequestDto dto)
    {
        var user = await userRepository.GetByEmailAsync(dto.Email);

        if (user is null || !BCrypt.Net.BCrypt.Verify(dto.Password, user.PasswordHash))
            throw new UnauthorizedAccessException("Invalid credentials.");

        var token = tokenService.GenerateToken(user);
        return user.ToAuthResponseDto(token);
    }
}
