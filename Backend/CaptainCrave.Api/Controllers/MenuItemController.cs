using Api.DTOs;
using Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace Api.Controllers;

// Håndterer HTTP-requests relateret til menu-items.
[ApiController]
[Route("api/[controller]")]
public class MenuItemsController(IMenuItemService menuItemService, IRestaurantService restaurantService, IMenuService menuService, IImageStorageService imageStorageService) : ControllerBase
{
    private readonly IMenuItemService _menuItemService = menuItemService;
    private readonly IRestaurantService _restaurantService = restaurantService;
    private readonly IMenuService _menuService = menuService;
    private readonly IImageStorageService _imageStorageService = imageStorageService;

    // Henter alle menu-items for den angivne restaurant.
    [HttpGet("restaurant/{restaurantId:int}")]
    public async Task<IActionResult> GetByRestaurantId(int restaurantId)
    {
        if (restaurantId <= 0)
            return BadRequest();

        var items = await _menuItemService.GetByRestaurantIdAsync(restaurantId);
        return Ok(items);
    }

    // Henter alle menu-items for den angivne menu.
    [HttpGet("menu/{menuId:int}")]
    public async Task<IActionResult> GetByMenuId(int menuId)
    {
        if (menuId <= 0)
            return BadRequest();

        var items = await _menuItemService.GetByMenuIdAsync(menuId);
        return Ok(items);
    }

    // Henter den aktuelle brugers ID fra JWT-tokenet.
    private int? GetCurrentUserId()
    {
        var claimValue = User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? User.FindFirstValue(JwtRegisteredClaimNames.Sub);

        return int.TryParse(claimValue, out var userId) ? userId : null;
    }

    // Opretter et nyt menu-item og returnerer det med status 201 Created.
    // Restaurantbrugere kan kun oprette menu-items i deres egen restaurants menuer.
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

    // Opdaterer et eksisterende menu-item, hvis brugeren har adgang til det.
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

    // Uploader eller erstatter billedet på et menu-item.
    // Billedet gemmes lokalt under wwwroot/uploads.
    [HttpPost("{id}/image")]
    [Authorize(Roles = "Restaurant,Admin")]
    [RequestSizeLimit(5_000_000)]
    public async Task<IActionResult> UploadImage(int id, IFormFile file)
    {
        // checker om brugeren er logget ind og har de nødvendige rettigheder.
        var userId = GetCurrentUserId();
        if (userId is null)
            return Unauthorized();

        string relativeUrl; 

        // Gemmer billedet lokalt under wwwroot/uploads/menu-items.
        try
        {
            // Gemmer billedet og får den relative URL tilbage. 
            relativeUrl = await _imageStorageService.SaveAsync(file, "menu-items");
        }
        catch (InvalidOperationException ex)
        {
            // returnerer en 400 Bad Request med fejlbeskeden, 
            // fx Unsupported file type eller File is empty or exceeds the 5 MB limit.
            return BadRequest(new { message = ex.Message });
        }

        // Opdaterer menu-item'et med den nye billed-URL. 
        // rollen tjekkes for at sikre, at brugeren har de nødvendige rettigheder.
        var updated = await _menuItemService.UpdateImageUrlAsync(id, relativeUrl, userId.Value, User.IsInRole("Admin"));
        if (updated is null)
        {
            // hvis opdateringen af menu-item'et mislykkedes, slettes det uploadede billede.
            _imageStorageService.Delete(relativeUrl);
            return NotFound();
        }
        
        return Ok(updated); // Returnerer den opdaterede menu-item med den nye billed-URL.
    }

    // Soft deleter et menu-item, hvis brugeren har adgang til det.
    // Menu-item'et skjules, men fjernes ikke permanent.
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

    // Gendanner et tidligere soft-deleted menu-item, hvis brugeren har adgang til det.
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

    // Sletter et menu-item permanent, hvis brugeren har adgang til det.
    // Denne handling kan ikke fortrydes.
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

    // Henter soft-deleted menu-items for en restaurant, så de kan vises og eventuelt gendannes.
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
