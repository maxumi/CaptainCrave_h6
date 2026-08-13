using Api.DTOs;
using Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace Api.Controllers;

// Handles requests related to restaurants. 
[ApiController]
[Route("api/[controller]")]
public class RestaurantsController(IRestaurantService restaurantService, IMenuItemService menuItemService) : ControllerBase
{
    private readonly IRestaurantService _restaurantService = restaurantService;
    private readonly IMenuItemService _menuItemService = menuItemService;

    // Retrieves the logged-in user's ID from the JWT token.
    private int? GetCurrentUserId()
    {
        var claimValue = User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? User.FindFirstValue(JwtRegisteredClaimNames.Sub);

        return int.TryParse(claimValue, out var userId) ? userId : null;
    }

    // Returns all restaurants.
    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var restaurants = await _restaurantService.GetAllAsync();
        return Ok(restaurants);
    }

    // Returns restaurants within the specified radius of the given latitude and longitude.
    [HttpGet("nearby")]
    public async Task<IActionResult> GetNearby(
        [FromQuery] double latitude,
        [FromQuery] double longitude,
        [FromQuery] double radiusKm = 10)
    {
        var restaurants =
            await _restaurantService.GetNearbyRestaurantsAsync(
                latitude,
                longitude,
                radiusKm);

        return Ok(restaurants);
    }

    // Returns a single restaurant by ID, or 404 if not found.
    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id)
    {
        var restaurant = await _restaurantService.GetByIdAsync(id);
        if (restaurant is null)
            return NotFound();

        return Ok(restaurant);
    }
    
    // Returns the restaurant profile that belongs
    // to the currently logged-in restaurant user.
    [HttpGet("me")]
    [Authorize(Roles = "Restaurant,Admin")]
    public async Task<IActionResult> GetMine()
    {
        var userId = GetCurrentUserId();
        if (userId is null)
            return Unauthorized();

        var restaurant = await _restaurantService.GetByUserIdAsync(userId.Value);
        if (restaurant is null)
            return NotFound();

        return Ok(restaurant);
    }

    // Returns all menu items for the specified restaurant.
    [HttpGet("{id}/menu-items")]
    public async Task<IActionResult> GetMenuItems(int id)
    {
        var items = await _menuItemService.GetByRestaurantIdAsync(id);
        return Ok(items);
    }

    // Creates a new restaurant and returns it with a 201 status.
    [HttpPost]
    [Authorize(Roles = "Restaurant,Admin")]
    public async Task<IActionResult> Create(CreateRestaurantDto dto)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var userId = GetCurrentUserId();
        if (userId is null)
            return Unauthorized();

        var existingRestaurant = await _restaurantService.GetByUserIdAsync(userId.Value);
        if (existingRestaurant is not null)
            return Conflict(new { message = "Restaurant profile already exists for this account." });

        // Always bind a new restaurant to the authenticated user.
        dto.UserId = userId.Value;

        var created = await _restaurantService.CreateAsync(dto);
        return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
    }
}