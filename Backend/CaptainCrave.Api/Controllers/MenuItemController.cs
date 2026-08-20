using Api.DTOs;
using Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace Api.Controllers;

// Handles HTTP requests for menu item resources.
[ApiController]
[Route("api/[controller]")]
public class MenuItemsController(IMenuItemService menuItemService, IRestaurantService restaurantService, IMenuService menuService, IImageStorageService imageStorageService) : ControllerBase
{
    private readonly IMenuItemService _menuItemService = menuItemService;
    private readonly IRestaurantService _restaurantService = restaurantService;
    private readonly IMenuService _menuService = menuService;
    private readonly IImageStorageService _imageStorageService = imageStorageService;

    // Returns all menu items for the specified restaurant.
    [HttpGet("restaurant/{restaurantId:int}")]
    public async Task<IActionResult> GetByRestaurantId(int restaurantId)
    {
        if (restaurantId <= 0)
            return BadRequest();

        var items = await _menuItemService.GetByRestaurantIdAsync(restaurantId);
        return Ok(items);
    }

    // Returns all menu items for the specified menu.
    [HttpGet("menu/{menuId:int}")]
    public async Task<IActionResult> GetByMenuId(int menuId)
    {
        if (menuId <= 0)
            return BadRequest();

        var items = await _menuItemService.GetByMenuIdAsync(menuId);
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
            if (restaurant is null)
                return NotFound();

            var menu = await _menuService.GetByIdAsync(dto.MenuId);
            if (menu is null || menu.RestaurantId != restaurant.Id)
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

    // Uploads (or replaces) a menu item's image and stores it on local disk under wwwroot/uploads.
    [HttpPost("{id}/image")]
    [Authorize(Roles = "Restaurant,Admin")]
    [RequestSizeLimit(5_000_000)]
    public async Task<IActionResult> UploadImage(int id, IFormFile file)
    {
        var userId = GetCurrentUserId();
        if (userId is null)
            return Unauthorized();

        string relativeUrl;
        try
        {
            relativeUrl = await _imageStorageService.SaveAsync(file, "menu-items");
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }

        var updated = await _menuItemService.UpdateImageUrlAsync(id, relativeUrl, userId.Value, User.IsInRole("Admin"));
        if (updated is null)
        {
            _imageStorageService.Delete(relativeUrl);
            return NotFound();
        }

        return Ok(updated);
    }

    // Deletes an existing menu item if the caller is authorized.
    // This is a soft delete: the item is hidden, not removed, and can be restored.
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

    // Restores a previously soft-deleted menu item if the caller is authorized.
    [HttpPost("{id}/restore")]
    [Authorize(Roles = "Restaurant,Admin")]
    public async Task<IActionResult> Restore(int id)
    {
        var userId = GetCurrentUserId();
        if (userId is null)
            return Unauthorized();

        var restored = await _menuItemService.RestoreAsync(id, userId.Value, User.IsInRole("Admin"));
        if (!restored)
            return NotFound();

        return NoContent();
    }

    // Permanently deletes a menu item (soft-deleted or not) if the caller is authorized. This cannot be undone.
    [HttpDelete("{id}/permanent")]
    [Authorize(Roles = "Restaurant,Admin")]
    public async Task<IActionResult> HardDelete(int id)
    {
        var userId = GetCurrentUserId();
        if (userId is null)
            return Unauthorized();

        try
        {
            var deleted = await _menuItemService.HardDeleteAsync(id, userId.Value, User.IsInRole("Admin"));
            if (!deleted)
                return NotFound();

            return NoContent();
        }
        catch (DbUpdateException)
        {
            return Conflict(new { message = "Cannot permanently delete this menu item because it still appears on past orders." });
        }
    }

    // Returns the soft-deleted menu items for a restaurant, so they can be reviewed and restored.
    [HttpGet("restaurant/{restaurantId:int}/deleted")]
    [Authorize(Roles = "Restaurant,Admin")]
    public async Task<IActionResult> GetDeleted(int restaurantId)
    {
        var userId = GetCurrentUserId();
        if (userId is null)
            return Unauthorized();

        var items = await _menuItemService.GetDeletedByRestaurantIdAsync(restaurantId, userId.Value, User.IsInRole("Admin"));
        if (items is null)
            return StatusCode(StatusCodes.Status403Forbidden);

        return Ok(items);
    }
}