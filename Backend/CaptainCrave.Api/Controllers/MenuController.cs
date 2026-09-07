using Api.DTOs;
using Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace Api.Controllers;

// Håndterer HTTP-requests relateret til menuer.
[ApiController]
[Route("api/[controller]")]
public class MenusController(IMenuService menuService, IRestaurantService restaurantService) : ControllerBase
{
    private readonly IMenuService _menuService = menuService;
    private readonly IRestaurantService _restaurantService = restaurantService;

    // Henter den aktuelle brugers ID fra JWT-tokenet.
    private int? GetCurrentUserId()
    {
        var claimValue = User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? User.FindFirstValue(JwtRegisteredClaimNames.Sub);

        return int.TryParse(claimValue, out var userId) ? userId : null;
    }

    /// <summary>
    /// Henter alle menuer for den angivne restaurant.
    /// </summary>
    /// <param name="restaurantId">ID på restauranten.</param>
    [HttpGet("restaurant/{restaurantId}")]
    public async Task<IActionResult> GetByRestaurant(int restaurantId)
    {
        var menus = await _menuService.GetByRestaurantIdAsync(restaurantId);
        return Ok(menus);
    }

    /// <summary>
    /// Henter en enkelt menu ud fra dens ID.
    /// Returnerer 404 Not Found, hvis menuen ikke findes.
    /// </summary>
    /// <param name="id">ID på menuen.</param>
    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id)
    {
        var menu = await _menuService.GetByIdAsync(id);
        if (menu is null)
            return NotFound();

        return Ok(menu);
    }

    /// <summary>
    /// Opretter en ny menu og returnerer den med status 201 Created.
    /// Restaurantbrugere kan kun oprette menuer til deres egen restaurant,
    /// mens administratorer kan oprette menuer til alle restauranter.
    /// </summary>
    /// <param name="dto">Oplysninger om restaurant og navn på menuen.</param>
    [HttpPost]
    [Authorize(Roles = "Restaurant,Admin")]
    public async Task<IActionResult> Create(CreateMenuDto dto)
    {
        // Controllerens flow: valider requestet, tjek adgang, kald Service-laget
        // og oversæt resultatet til et passende HTTP-svar.
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        if (User.IsInRole("Restaurant"))
        {
            var userId = GetCurrentUserId();
            if (userId is null)
                return Unauthorized();

            var restaurant = await _restaurantService.GetByUserIdAsync(userId.Value);
            // Rollen "Restaurant" er ikke nok; brugeren skal eje netop denne restaurant.
            // NotFound afslører samtidig ikke, om en anden restaurants id findes.
            if (restaurant is null || restaurant.Id != dto.RestaurantId)
                return NotFound();
        }

        var created = await _menuService.CreateAsync(dto);
        // 201 Created fortæller, at ressourcen er oprettet, og peger på GetById.
        return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
    }

    /// <summary>
    /// Soft deleter en menu, hvis brugeren ejer restauranten eller er administrator.
    /// Menuen og dens menu-items skjules, men fjernes ikke permanent.
    /// </summary>
    /// <param name="id">ID på menuen der skal slettes.</param>
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
    /// Gendanner en tidligere soft-deleted menu, hvis brugeren ejer restauranten
    /// eller er administrator.
    /// </summary>
    /// <param name="id">ID på menuen der skal gendannes.</param>
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
    /// Sletter en menu permanent, hvis brugeren ejer restauranten eller er administrator.
    /// Denne handling kan ikke fortrydes.
    /// </summary>
    /// <param name="id">ID på menuen der skal slettes permanent.</param>
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
    /// Henter soft-deleted menuer for en restaurant, så de kan vises
    /// og eventuelt gendannes.
    /// </summary>
    /// <param name="restaurantId">ID på restauranten.</param>
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
