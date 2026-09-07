using Api.DTOs;
using Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace Api.Controllers;

// Håndterer requests relateret til restauranter.
[ApiController]
[Route("api/[controller]")]
public class RestaurantsController(IRestaurantService restaurantService, IMenuItemService menuItemService, IMenuService menuService, IImageStorageService imageStorageService) : ControllerBase
{
    private readonly IRestaurantService _restaurantService = restaurantService;
    private readonly IMenuItemService _menuItemService = menuItemService;
    private readonly IMenuService _menuService = menuService;
    private readonly IImageStorageService _imageStorageService = imageStorageService;

    // Henter den aktuelle brugers ID fra JWT-tokenet.
    private int? GetCurrentUserId()
    {
        var claimValue = User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? User.FindFirstValue(JwtRegisteredClaimNames.Sub);

        return int.TryParse(claimValue, out var userId) ? userId : null;
    }

    // Henter alle aktive restauranter.
    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var restaurants = await _restaurantService.GetAllAsync();
        return Ok(restaurants);
    }

    // Henter restauranter inden for den angivne radius fra en geografisk position.
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

    // Henter en enkelt restaurant ud fra ID, eller returnerer 404 hvis ikke fundet.
    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id)
    {
        var restaurant = await _restaurantService.GetByIdAsync(id);
        if (restaurant is null)
            return NotFound();

        return Ok(restaurant);
    }

    // Henter restaurantprofilen, der tilhører den aktuelle restaurantbruger.
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

    // Henter alle menu-items for den angivne restaurant.
    [HttpGet("{id}/menu-items")]
    public async Task<IActionResult> GetMenuItems(int id)
    {
        var items = await _menuItemService.GetByRestaurantIdAsync(id);
        return Ok(items);
    }

    // Henter alle menuer for den angivne restaurant.
    [HttpGet("{id}/menus")]
    public async Task<IActionResult> GetMenus(int id)
    {
        var menus = await _menuService.GetByRestaurantIdAsync(id);
        return Ok(menus);
    }

    // Opretter en ny restaurant og knytter den til den autentificerede bruger.
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

        // Sikrer at en ny restaurant altid knyttes til den autentificerede bruger.
        dto.UserId = userId.Value;

        var created = await _restaurantService.CreateAsync(dto);
        return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
    }

    // Opdaterer de redigerbare oplysninger på en restaurant.
    [HttpPut("{id}")]
    [Authorize(Roles = "Restaurant,Admin")]
    public async Task<IActionResult> Update(int id, UpdateRestaurantDto dto)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var userId = GetCurrentUserId();
        if (userId is null)
            return Unauthorized();

        var updated = await _restaurantService.UpdateAsync(id, dto, userId.Value, User.IsInRole("Admin"));
        if (updated is null)
            return NotFound();

        return Ok(updated);
    }

    // Uploader eller erstatter restaurantens billede.
    // Billedet gemmes lokalt under wwwroot/uploads.
    [HttpPost("{id}/image")]
    [Authorize(Roles = "Restaurant,Admin")]
    [RequestSizeLimit(5_000_000)]
    public async Task<IActionResult> UploadImage(int id, IFormFile file)
    {
        var userId = GetCurrentUserId();
        if (userId is null)
            return Unauthorized();

        var restaurant = await _restaurantService.GetByIdAsync(id);
        if (restaurant is null)
            return NotFound();

        // Kun ejeren af restauranten eller en administrator må ændre billedet.
        if (!User.IsInRole("Admin") && restaurant.UserId != userId)
            return Forbid();

        string relativeUrl;
        try
        {
            relativeUrl = await _imageStorageService.SaveAsync(file, "restaurants");
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }

        var updated = await _restaurantService.UpdateImageUrlAsync(id, relativeUrl, userId.Value, User.IsInRole("Admin"));
        if (updated is null)
        {
            // Fjerner den uploadede fil igen, hvis databasen ikke kunne opdateres.
            _imageStorageService.Delete(relativeUrl);
            return NotFound();
        }

        return Ok(updated);
    }

    // Soft deleter en restaurant, hvis brugeren ejer den eller er administrator.
    // Restauranten, dens menuer og menu-items skjules, men fjernes ikke permanent.
    [HttpDelete("{id}")]
    [Authorize(Roles = "Restaurant,Admin")]
    public async Task<IActionResult> Delete(int id)
    {
        var userId = GetCurrentUserId();
        if (userId is null)
            return Unauthorized();

        var deleted = await _restaurantService.DeleteAsync(id, userId.Value, User.IsInRole("Admin"));
        if (!deleted)
            return NotFound();

        return NoContent();
    }

    // Gendanner en tidligere soft-deleted restaurant.
    [HttpPost("{id}/restore")]
    [Authorize(Roles = "Restaurant,Admin")]
    public async Task<IActionResult> Restore(int id)
    {
        var userId = GetCurrentUserId();
        if (userId is null)
            return Unauthorized();

        var restored = await _restaurantService.RestoreAsync(id, userId.Value, User.IsInRole("Admin"));
        if (!restored)
            return NotFound();

        return NoContent();
    }

    // Sletter en restaurant permanent, hvis brugeren ejer den eller er administrator.
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
            var deleted = await _restaurantService.HardDeleteAsync(id, userId.Value, User.IsInRole("Admin"));
            if (!deleted)
                return NotFound();

            return NoContent();
        }
        catch (DbUpdateException)
        {
            return Conflict(new { message = "Cannot permanently delete this restaurant because it still has related data, such as past orders." });
        }
    }

    // Henter alle soft-deleted restauranter til administratorens oversigt over slettede data.
    [HttpGet("deleted")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> GetDeleted()
    {
        var restaurants = await _restaurantService.GetDeletedAsync();
        return Ok(restaurants);
    }
}