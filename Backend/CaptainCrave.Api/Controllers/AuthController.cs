using Api.DTOs.Auth;
using Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace Api.Controllers;

// Handles user registration and login requests
[ApiController]
[Route("api/auth")]
public class AuthController(IAuthService authService) : ControllerBase
{
    // Creates a new user account. 
    // Returns 201 Created if successful or 409 Conflict if the email already exists.
    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterRequestDto dto)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        try
        {
            // The controller forwards the data to AuthService, which creates the user in the database
            var response = await authService.RegisterAsync(dto);
            return CreatedAtAction(nameof(Register), response);
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new { message = ex.Message });
        }
    }

    // Verifies the user's email and password.
    // Returns a JWT token if the login is successful.
    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequestDto dto)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        try
        {
            //AuthService checks whether the email address and password match a user in the database and generates a JWT token if they do
            var response = await authService.LoginAsync(dto);
            return Ok(response);
        }
        catch (UnauthorizedAccessException)
        {
            return Unauthorized(new { message = "Invalid email or password." });
        }
    }
}