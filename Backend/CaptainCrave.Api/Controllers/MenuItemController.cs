using Api.DTOs;
using Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace Api.Controllers;

// Handles HTTP requests for menu item resources.
[ApiController]
[Route("api/[controller]")]
public class MenuItemsController(IMenuItemService menuItemService, IRestaurantService restaurantService) : ControllerBase
{
    private readonly IMenuItemService _menuItemService = menuItemService;
    private readonly IRestaurantService _restaurantService = restaurantService;

    // Returns all menu items for the specified restaurant.
    [HttpGet("restaurant/{restaurantId:int}")]
    public async Task<IActionResult> GetByRestaurantId(int restaurantId)
    {
        if (restaurantId <= 0)
            return BadRequest();

        var items = await _menuItemService.GetByRestaurantIdAsync(restaurantId);
        return Ok(items);
    }

    private int? GetCurrentUserId()
    {
        var claimValue = User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? User.FindFirstValue(JwtRegisteredClaimNames.Sub);

        return int.TryParse(claimValue, out var userId) ? userId : null;
    }

    // Creates a new menu item and returns it with a 201 status.
    [HttpPost]
    [Authorize(Roles = "Restaurant,Admin")]
    public async Task<IActionResult> Create(CreateMenuItemDto dto)
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

        var created = await _menuItemService.CreateAsync(dto);
        return Created(string.Empty, created);
    }

    // Updates an existing menu item if the caller is authorized.
    [HttpPut("{id}")]
    [Authorize(Roles = "Restaurant,Admin")]
    public async Task<IActionResult> Update(int id, CreateMenuItemDto dto)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var userId = GetCurrentUserId();
        if (userId is null)
            return Unauthorized();

        var updated = await _menuItemService.UpdateAsync(id, dto, userId.Value, User.IsInRole("Admin"));
        if (updated is null)
            return NotFound();

        return Ok(updated);
    }

    // Deletes an existing menu item if the caller is authorized.
    [HttpDelete("{id}")]
    [Authorize(Roles = "Restaurant,Admin")]
    public async Task<IActionResult> Delete(int id)
    {
        var userId = GetCurrentUserId();
        if (userId is null)
            return Unauthorized();

        var deleted = await _menuItemService.DeleteAsync(id, userId.Value, User.IsInRole("Admin"));
        if (!deleted)
            return NotFound();

        return NoContent();
    }
}