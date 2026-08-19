using Api.DTOs;
using Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
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

    /// <summary>
    /// Returns all menus for the specified restaurant.
    /// </summary>
    /// <param name="restaurantId">Route value identifying the restaurant.</param>
    [HttpGet("restaurant/{restaurantId}")]
    public async Task<IActionResult> GetByRestaurant(int restaurantId)
    {
        var menus = await _menuService.GetByRestaurantIdAsync(restaurantId);
        return Ok(menus);
    }

    /// <summary>
    /// Returns a single menu by ID, or 404 if not found.
    /// </summary>
    /// <param name="id">Route value identifying the menu.</param>
    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id)
    {
        var menu = await _menuService.GetByIdAsync(id);
        if (menu is null)
            return NotFound();

        return Ok(menu);
    }

    /// <summary>
    /// Creates a new menu and returns it with a 201 status. Restaurant users may only
    /// create menus for the restaurant they own; Admins can target any restaurant.
    /// </summary>
    /// <param name="dto">The requested menu's restaurant id and name.</param>
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

    /// <summary>
    /// Deletes a menu when the caller owns its restaurant or is an admin.
    /// This is a soft delete: the menu (and its menu items) are hidden, not removed, and can be restored.
    /// </summary>
    /// <param name="id">The menu id to delete.</param>
    [HttpDelete("{id}")]
    [Authorize(Roles = "Restaurant,Admin")]
    public async Task<IActionResult> Delete(int id)
    {
        var userId = GetCurrentUserId();
        if (userId is null)
            return Unauthorized();

        var deleted = await _menuService.DeleteAsync(id, userId.Value, User.IsInRole("Admin"));
        if (!deleted)
            return NotFound();

        return NoContent();
    }

    /// <summary>
    /// Restores a previously soft-deleted menu when the caller owns its restaurant or is an admin.
    /// </summary>
    /// <param name="id">The menu id to restore.</param>
    [HttpPost("{id}/restore")]
    [Authorize(Roles = "Restaurant,Admin")]
    public async Task<IActionResult> Restore(int id)
    {
        var userId = GetCurrentUserId();
        if (userId is null)
            return Unauthorized();

        var restored = await _menuService.RestoreAsync(id, userId.Value, User.IsInRole("Admin"));
        if (!restored)
            return NotFound();

        return NoContent();
    }

    /// <summary>
    /// Permanently deletes a menu (soft-deleted or not) when the caller owns its restaurant or is an admin.
    /// This cannot be undone.
    /// </summary>
    /// <param name="id">The menu id to permanently delete.</param>
    [HttpDelete("{id}/permanent")]
    [Authorize(Roles = "Restaurant,Admin")]
    public async Task<IActionResult> HardDelete(int id)
    {
        var userId = GetCurrentUserId();
        if (userId is null)
            return Unauthorized();

        try
        {
            var deleted = await _menuService.HardDeleteAsync(id, userId.Value, User.IsInRole("Admin"));
            if (!deleted)
                return NotFound();

            return NoContent();
        }
        catch (DbUpdateException)
        {
            return Conflict(new { message = "Cannot permanently delete this menu because related data still references it." });
        }
    }

    /// <summary>
    /// Returns the soft-deleted menus for a restaurant, so they can be reviewed and restored.
    /// </summary>
    /// <param name="restaurantId">The owning restaurant's id.</param>
    [HttpGet("restaurant/{restaurantId}/deleted")]
    [Authorize(Roles = "Restaurant,Admin")]
    public async Task<IActionResult> GetDeleted(int restaurantId)
    {
        var userId = GetCurrentUserId();
        if (userId is null)
            return Unauthorized();

        var menus = await _menuService.GetDeletedByRestaurantIdAsync(restaurantId, userId.Value, User.IsInRole("Admin"));
        if (menus is null)
            return StatusCode(StatusCodes.Status403Forbidden);

        return Ok(menus);
    }
}
