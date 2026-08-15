using Api.DTOs;
using Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace Api.Controllers;

// Handles HTTP requests for menu resources.
[ApiController]
[Route("api/[controller]")]
public class MenusController(IMenuService menuService, IRestaurantService restaurantService) : ControllerBase
{
    private readonly IMenuService _menuService = menuService;
    private readonly IRestaurantService _restaurantService = restaurantService;

    // Retrieves the logged-in user's ID from the JWT token.
    private int? GetCurrentUserId()
    {
        var claimValue = User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? User.FindFirstValue(JwtRegisteredClaimNames.Sub);

        return int.TryParse(claimValue, out var userId) ? userId : null;
    }

    // Returns all menus for the specified restaurant.
    [HttpGet("restaurant/{restaurantId}")]
    public async Task<IActionResult> GetByRestaurant(int restaurantId)
    {
        var menus = await _menuService.GetByRestaurantIdAsync(restaurantId);
        return Ok(menus);
    }

    // Returns a single menu by ID, or 404 if not found.
    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id)
    {
        var menu = await _menuService.GetByIdAsync(id);
        if (menu is null)
            return NotFound();

        return Ok(menu);
    }

    // Creates a new menu and returns it with a 201 status.
    [HttpPost]
    [Authorize(Roles = "Restaurant,Admin")]
    public async Task<IActionResult> Create(CreateMenuDto dto)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        if (User.IsInRole("Restaurant"))
        {
            var userId = GetCurrentUserId();
            if (userId is null)
                return Unauthorized();

            var restaurant = await _restaurantService.GetByUserIdAsync(userId.Value);
            if (restaurant is null || restaurant.Id != dto.RestaurantId)
                return NotFound();
        }

        var created = await _menuService.CreateAsync(dto);
        return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
    }
}
