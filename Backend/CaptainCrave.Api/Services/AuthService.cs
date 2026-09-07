using Api.DTOs.Auth;
using Api.Mappers;
using Api.Models;
using Microsoft.AspNetCore.Identity;

namespace Api.Services;

// Håndterer forretningslogikken for at oprette og logge brugere ind.
public class AuthService(
    ITokenService tokenService,
    UserManager<User> userManager) : IAuthService
{
    /// <summary>
    /// Opretter en helt ny bruger via ASP.NET Identity, som også sørger for at hashe
    /// (kryptere) kodeordet, så det aldrig gemmes i klartekst. Bagefter får brugeren
    /// et JWT-token med det samme, så man er logget ind lige efter man har oprettet sig.
    /// </summary>
    /// <returns>Brugerens data plus et JWT-token, klar til at blive sendt til klienten.</returns>
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

    /// <summary>
    /// Finder brugeren ud fra e-mailen og tjekker, om kodeordet passer (via ASP.NET Identity).
    /// Passer det, får brugeren et nyt JWT-token tilbage, som beviser at man er logget ind.
    /// </summary>
    /// <returns>Brugerens data plus et JWT-token, hvis e-mail og kodeord passer sammen.</returns>
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
