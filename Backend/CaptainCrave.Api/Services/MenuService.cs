using Api.DTOs;
using Api.Mappers;
using Api.Repositories;

namespace Api.Services;

// Håndterer forretningslogikken for menu-operationer.
public class MenuService(IMenuRepository menuRepository, IRestaurantRepository restaurantRepository) : IMenuService
{
    private readonly IMenuRepository _menuRepository = menuRepository;
    private readonly IRestaurantRepository _restaurantRepository = restaurantRepository;

    /// <summary>
    /// Henter alle menuer, der hører til en restaurant, og pakker dem om til DTO'er.
    /// </summary>
    /// <returns>Alle menuer for restauranten (kan være en tom liste).</returns>
    public async Task<IEnumerable<MenuDto>> GetByRestaurantIdAsync(int restaurantId)
    {
        var menus = await _menuRepository.GetByRestaurantIdAsync(restaurantId);
        return menus.Select(m => m.ToDto());
    }

    /// <summary>
    /// Henter én bestemt menu ud fra dens id.
    /// </summary>
    /// <returns>Menuen som DTO, eller null hvis den ikke findes.</returns>
    public async Task<MenuDto?> GetByIdAsync(int id)
    {
        var menu = await _menuRepository.GetByIdAsync(id);
        return menu?.ToDto();
    }

    /// <summary>
    /// Henter én menu ud fra id, også selvom den er blevet soft-deleted (lagt i papirkurven).
    /// Bruges internt, når vi f.eks. skal tjekke ejerskab for en slettet menu.
    /// </summary>
    /// <returns>Menuen som DTO (uanset om den er slettet), eller null hvis den slet ikke findes.</returns>
    public async Task<MenuDto?> GetByIdIncludingDeletedAsync(int id)
    {
        var menu = await _menuRepository.GetByIdIncludingDeletedAsync(id);
        return menu?.ToDto();
    }

    /// <summary>
    /// Henter de menuer, der er blevet slettet (soft delete) for en restaurant,
    /// men kun hvis brugeren har lov til at se dem.
    /// </summary>
    /// <returns>De slettede menuer, eller null hvis brugeren ikke må se dem.</returns>
    public async Task<IEnumerable<MenuDto>?> GetDeletedByRestaurantIdAsync(int restaurantId, int userId, bool isAdmin)
    {
        if (!isAdmin && !await UserOwnsRestaurantAsync(userId, restaurantId))
            return null;

        var menus = await _menuRepository.GetDeletedByRestaurantIdAsync(restaurantId);
        return menus.Select(m => m.ToDto());
    }

    /// <summary>
    /// Pakker DTO'en om til en rigtig Menu-model, gemmer den i databasen,
    /// og giver den nye menu tilbage som DTO (nu med et rigtigt Id).
    /// </summary>
    /// <returns>Den nyoprettede menu som DTO.</returns>
    public async Task<MenuDto> CreateAsync(CreateMenuDto dto)
    {
        var menu = dto.ToMenu();
        var created = await _menuRepository.CreateAsync(menu);
        return created.ToDto();
    }

    /// <summary>
    /// Sletter en menu (soft delete), men kun hvis brugeren ejer restauranten bag den, eller
    /// er admin. Menuen bliver bare skjult, ikke rigtigt slettet, så den kan gendannes senere.
    /// </summary>
    /// <returns>True hvis den blev slettet, false hvis den ikke findes eller brugeren ikke må.</returns>
    public async Task<bool> DeleteAsync(int id, int userId, bool isAdmin)
    {
        var menu = await _menuRepository.GetByIdAsync(id);
        if (menu is null)
            return false;

        if (!isAdmin && !await UserOwnsRestaurantAsync(userId, menu.RestaurantId))
            return false;

        await _menuRepository.SoftDeleteAsync(menu);
        return true;
    }

    /// <summary>
    /// Gendanner en menu, der tidligere er blevet slettet, så den kommer tilbage til live,
    /// men kun hvis brugeren ejer restauranten eller er admin.
    /// </summary>
    /// <returns>True hvis den blev gendannet, false hvis den ikke findes, ikke var slettet, eller brugeren ikke må.</returns>
    public async Task<bool> RestoreAsync(int id, int userId, bool isAdmin)
    {
        var menu = await _menuRepository.GetByIdIncludingDeletedAsync(id);
        if (menu is null || !menu.IsDeleted)
            return false;

        if (!isAdmin && !await UserOwnsRestaurantAsync(userId, menu.RestaurantId))
            return false;

        await _menuRepository.RestoreAsync(menu);
        return true;
    }

    /// <summary>
    /// Sletter en menu FOR ALTID, uanset om den var soft-deleted eller ej, og kun hvis
    /// brugeren ejer restauranten eller er admin. Der er ingen fortryd-knap efter dette.
    /// </summary>
    /// <returns>True hvis den blev slettet permanent, false hvis den ikke findes eller brugeren ikke må.</returns>
    public async Task<bool> HardDeleteAsync(int id, int userId, bool isAdmin)
    {
        var menu = await _menuRepository.GetByIdIncludingDeletedAsync(id);
        if (menu is null)
            return false;

        if (!isAdmin && !await UserOwnsRestaurantAsync(userId, menu.RestaurantId))
            return false;

        await _menuRepository.HardDeleteAsync(menu);
        return true;
    }

    /// <summary>
    /// Tjekker om brugeren har en restaurant med netop dette id blandt sine egne restauranter.
    /// </summary>
    /// <returns>True, hvis restauranten findes blandt brugerens egne restauranter. Ellers false.</returns>
    private async Task<bool> UserOwnsRestaurantAsync(int userId, int restaurantId)
    {
        var restaurants = await _restaurantRepository.GetByUserIdAsync(userId);
        return restaurants.Any(r => r.Id == restaurantId);
    }
}
