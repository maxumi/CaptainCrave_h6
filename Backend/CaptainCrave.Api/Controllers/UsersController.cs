using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Api.DTOs;
using Api.Repositories;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Api.Controllers;

// Handles requests related to the currently logged-in user.
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class UsersController(IUserRepository userRepository) : ControllerBase
{
    private readonly IUserRepository _userRepository = userRepository;

    private int? GetCurrentUserId()
    {
        var claimValue = User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? User.FindFirstValue(JwtRegisteredClaimNames.Sub);

        return int.TryParse(claimValue, out var userId) ? userId : null;
    }

    [HttpGet("me")]
    public async Task<IActionResult> GetMe()
    {
        var userId = GetCurrentUserId();
        if (userId is null)
            return Unauthorized();

        var user = await _userRepository.GetByIdAsync(userId.Value);
        if (user is null)
            return NotFound();

        return Ok(new UserProfileDto
        {
            Id = user.Id,
            Name = user.Name,
            Email = user.Email,
            Address = user.Address,
            Latitude = user.Latitude,
            Longitude = user.Longitude,
            Role = user.Role.ToString()
        });
    }

    [HttpPut("me")]
    public async Task<IActionResult> UpdateMe([FromBody] UpdateUserProfileDto dto)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var userId = GetCurrentUserId();
        if (userId is null)
            return Unauthorized();

        var updated = await _userRepository.UpdateProfileAsync(
            userId.Value,
            dto.Name,
            dto.Address,
            dto.Latitude,
            dto.Longitude);

        if (updated is null)
            return NotFound();

        return Ok(new UserProfileDto
        {
            Id = updated.Id,
            Name = updated.Name,
            Email = updated.Email,
            Address = updated.Address,
            Latitude = updated.Latitude,
            Longitude = updated.Longitude,
            Role = updated.Role.ToString()
        });
    }
}
