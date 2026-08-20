using Api.DTOs;
using Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace Api.Controllers;

// Handles HTTP requests for category resources.
[ApiController]
[Route("api/[controller]")]
public class CategoriesController(ICategoryService categoryService) : ControllerBase
{
    private readonly ICategoryService _categoryService = categoryService;

    // Retrieves the logged-in user's ID from the JWT token.
    private int? GetCurrentUserId()
    {
        var claimValue = User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? User.FindFirstValue(JwtRegisteredClaimNames.Sub);

        return int.TryParse(claimValue, out var userId) ? userId : null;
    }

    // Returns all categories belonging to the specified restaurant, across all of its menus.
    [HttpGet("restaurant/{restaurantId}")]
    public async Task<IActionResult> GetByRestaurant(int restaurantId)
    {
        var categories = await _categoryService.GetByRestaurantIdAsync(restaurantId);
        return Ok(categories);
    }

    // Returns all categories belonging to the specified menu.
    [HttpGet("menu/{menuId}")]
    public async Task<IActionResult> GetByMenu(int menuId)
    {
        var categories = await _categoryService.GetByMenuIdAsync(menuId);
        return Ok(categories);
    }

    // Creates a new category and returns it with a 201 status.
    [HttpPost]
    [Authorize(Roles = "Restaurant,Admin")]
    public async Task<IActionResult> Create(CreateCategoryDto dto)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var created = await _categoryService.CreateAsync(dto);
        return Created(string.Empty, created);
    }

    // Deletes a category if the caller is authorized. This is a soft delete: the category
    // is hidden, not removed, and can be restored.
    [HttpDelete("{id}")]
    [Authorize(Roles = "Restaurant,Admin")]
    public async Task<IActionResult> Delete(int id)
    {
        var userId = GetCurrentUserId();
        if (userId is null)
            return Unauthorized();

        var deleted = await _categoryService.DeleteAsync(id, userId.Value, User.IsInRole("Admin"));
        if (!deleted)
            return NotFound();

        return NoContent();
    }

    // Restores a previously soft-deleted category if the caller is authorized.
    [HttpPost("{id}/restore")]
    [Authorize(Roles = "Restaurant,Admin")]
    public async Task<IActionResult> Restore(int id)
    {
        var userId = GetCurrentUserId();
        if (userId is null)
            return Unauthorized();

        var restored = await _categoryService.RestoreAsync(id, userId.Value, User.IsInRole("Admin"));
        if (!restored)
            return NotFound();

        return NoContent();
    }

    // Permanently deletes a category (soft-deleted or not) if the caller is authorized. This cannot be undone.
    [HttpDelete("{id}/permanent")]
    [Authorize(Roles = "Restaurant,Admin")]
    public async Task<IActionResult> HardDelete(int id)
    {
        var userId = GetCurrentUserId();
        if (userId is null)
            return Unauthorized();

        try
        {
            var deleted = await _categoryService.HardDeleteAsync(id, userId.Value, User.IsInRole("Admin"));
            if (!deleted)
                return NotFound();

            return NoContent();
        }
        catch (DbUpdateException)
        {
            return Conflict(new { message = "Cannot permanently delete this category because menu items still reference it." });
        }
    }

    // Returns the soft-deleted categories for a restaurant, so they can be reviewed and restored.
    [HttpGet("restaurant/{restaurantId}/deleted")]
    [Authorize(Roles = "Restaurant,Admin")]
    public async Task<IActionResult> GetDeleted(int restaurantId)
    {
        var userId = GetCurrentUserId();
        if (userId is null)
            return Unauthorized();

        var categories = await _categoryService.GetDeletedByRestaurantIdAsync(restaurantId, userId.Value, User.IsInRole("Admin"));
        if (categories is null)
            return StatusCode(StatusCodes.Status403Forbidden);

        return Ok(categories);
    }
}
