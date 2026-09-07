using Api.DTOs.Auth;
using Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace Api.Controllers;

// Håndterer registrering og login af brugere.
[ApiController]
[Route("api/auth")]
public class AuthController(IAuthService authService) : ControllerBase
{
    // Opretter en ny brugerkonto.
    // Returnerer 201 Created ved succes eller 409 Conflict, hvis e-mailen allerede findes.
    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterRequestDto dto)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        try
        {
            // Sender registreringsdata videre til AuthService, som står for oprettelsen af brugeren.
            var response = await authService.RegisterAsync(dto);
            return CreatedAtAction(nameof(Register), response);
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new { message = ex.Message });
        }
    }

    // Kontrollerer brugerens e-mail og adgangskode.
    // Returnerer et JWT-token, hvis login lykkes.
    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequestDto dto)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        try
        {
            // AuthService validerer loginoplysningerne og genererer et JWT-token ved korrekt login.
            var response = await authService.LoginAsync(dto);
            return Ok(response);
        }
        catch (UnauthorizedAccessException)
        {
            return Unauthorized(new { message = "Invalid email or password." });
        }
    }
}