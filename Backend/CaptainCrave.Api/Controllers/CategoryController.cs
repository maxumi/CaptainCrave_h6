using Api.DTOs;
using Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace Api.Controllers;

// Håndterer HTTP-requests relateret til kategorier.
[ApiController]
[Route("api/[controller]")]
public class CategoriesController(ICategoryService categoryService) : ControllerBase
{
    private readonly ICategoryService _categoryService = categoryService;

    // Henter den aktuelle brugers ID fra JWT-tokenet.
    private int? GetCurrentUserId()
    {
        var claimValue = User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? User.FindFirstValue(JwtRegisteredClaimNames.Sub);

        return int.TryParse(claimValue, out var userId) ? userId : null;
    }

    // Henter alle kategorier, der tilhører den angivne restaurant, på tværs af dens menuer.
    [HttpGet("restaurant/{restaurantId}")]
    public async Task<IActionResult> GetByRestaurant(int restaurantId)
    {
        var categories = await _categoryService.GetByRestaurantIdAsync(restaurantId);
        return Ok(categories);
    }

    // Henter alle kategorier, der tilhører den angivne menu.
    [HttpGet("menu/{menuId}")]
    public async Task<IActionResult> GetByMenu(int menuId)
    {
        var categories = await _categoryService.GetByMenuIdAsync(menuId);
        return Ok(categories);
    }

    // Opretter en ny kategori og returnerer den med status 201 Created.
    [HttpPost]
    [Authorize(Roles = "Restaurant,Admin")]
    public async Task<IActionResult> Create(CreateCategoryDto dto)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var created = await _categoryService.CreateAsync(dto);
        return Created(string.Empty, created);
    }

    // Soft deleter en kategori, hvis brugeren har adgang til den.
    // Kategorien skjules, men fjernes ikke permanent og kan derfor gendannes.
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

    // Gendanner en tidligere soft-deleted kategori, hvis brugeren har adgang til den.
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

    // Sletter en kategori permanent, hvis brugeren har adgang til den.
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

    // Henter soft-deleted kategorier for en restaurant, så de kan vises og eventuelt gendannes.
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
