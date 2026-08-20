using Api.DTOs.Auth;
using Api.Mappers;
using Api.Models;
using Microsoft.AspNetCore.Identity;

namespace Api.Services;

// Handles the business logic for registering and logging in users
public class AuthService(
    ITokenService tokenService,
    UserManager<User> userManager) : IAuthService
{
    // Creates a new user using ASP.NET Identity and returns a JWT
    public async Task<AuthResponseDto> RegisterAsync(RegisterRequestDto dto)
    {
        var existingUser = await userManager.FindByEmailAsync(dto.Email);

        if (existingUser is not null)
            throw new InvalidOperationException("Email is already in use.");

        var user = dto.ToUser();

        user.UserName = dto.Email;
        user.Email = dto.Email;

        var result = await userManager.CreateAsync(user, dto.Password);

        if (!result.Succeeded)
        {
            var errors = string.Join(", ", result.Errors.Select(e => e.Description));
            throw new InvalidOperationException(errors);
        }

        var token = tokenService.GenerateToken(user);

        return user.ToAuthResponseDto(token);
    }

    // Finds the user by email, verifies the password using ASP.NET Identity and returns a JWT
    public async Task<AuthResponseDto> LoginAsync(LoginRequestDto dto)
    {
        var user = await userManager.FindByEmailAsync(dto.Email);

        if (user is null)
            throw new UnauthorizedAccessException("Invalid credentials.");

        var result = await userManager.CheckPasswordAsync(user, dto.Password);

        if (!result)
            throw new UnauthorizedAccessException("Invalid credentials.");

        var token = tokenService.GenerateToken(user);

        return user.ToAuthResponseDto(token);
    }
}
