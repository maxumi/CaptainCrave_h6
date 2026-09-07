using Api.DTOs;
using Api.Mappers;
using Api.Repositories;

namespace Api.Services;

// Håndterer forretningslogikken for menu-punkt (ret) operationer.
public class MenuItemService(IMenuItemRepository menuItemRepository, IRestaurantRepository restaurantRepository, IMenuService menuService, IImageStorageService imageStorageService) : IMenuItemService
{
    private readonly IMenuItemRepository _menuItemRepository = menuItemRepository;
    private readonly IRestaurantRepository _restaurantRepository = restaurantRepository;
    private readonly IMenuService _menuService = menuService;
    private readonly IImageStorageService _imageStorageService = imageStorageService;

    /// <summary>
    /// Henter alle retter, der hører til en restaurant, og pakker dem om til DTO'er.
    /// </summary>
    /// <returns>Alle retter for restauranten (kan være en tom liste).</returns>
    public async Task<IEnumerable<MenuItemDto>> GetByRestaurantIdAsync(int restaurantId)
    {
        var items = await _menuItemRepository.GetByRestaurantIdAsync(restaurantId);
        return items.Select(m => m.ToDto());
    }

    /// <summary>
    /// Henter alle retter, der hører til én bestemt menu, og pakker dem om til DTO'er.
    /// </summary>
    /// <returns>Alle retter for menuen (kan være en tom liste).</returns>
    public async Task<IEnumerable<MenuItemDto>> GetByMenuIdAsync(int menuId)
    {
        var items = await _menuItemRepository.GetByMenuIdAsync(menuId);
        return items.Select(m => m.ToDto());
    }

    /// <summary>
    /// Henter de retter, der er blevet slettet (soft delete, lagt i "papirkurven") for en
    /// restaurant, men kun hvis brugeren har lov til at se dem.
    /// </summary>
    /// <returns>De slettede retter, eller null hvis brugeren ikke må se dem.</returns>
    public async Task<IEnumerable<MenuItemDto>?> GetDeletedByRestaurantIdAsync(int restaurantId, int userId, bool isAdmin)
    {
        if (!isAdmin && !await UserOwnsRestaurantAsync(userId, restaurantId))
            return null;

        var items = await _menuItemRepository.GetDeletedByRestaurantIdAsync(restaurantId);
        return items.Select(m => m.ToDto());
    }

    /// <summary>
    /// Pakker DTO'en om til en rigtig MenuItem-model, gemmer den i databasen,
    /// og giver den nye ret tilbage som DTO (nu med et rigtigt Id).
    /// </summary>
    /// <returns>Den nyoprettede ret som DTO.</returns>
    public async Task<MenuItemDto> CreateAsync(CreateMenuItemDto dto)
    {
        var menuItem = dto.ToMenuItem();
        var created = await _menuItemRepository.CreateAsync(menuItem);
        return created.ToDto();
    }

    /// <summary>
    /// Opdaterer navn, beskrivelse, pris og andre felter på en ret, hvis brugeren ejer
    /// restauranten bag den, eller er admin. Kun en admin må flytte retten til en helt
    /// anden menu, en restaurant-bruger kan kun ændre retter på sine egne menuer.
    /// </summary>
    /// <returns>Den opdaterede ret som DTO, eller null hvis den ikke findes eller brugeren ikke må.</returns>
    public async Task<MenuItemDto?> UpdateAsync(int id, CreateMenuItemDto dto, int userId, bool isAdmin)
    {
        var existing = await _menuItemRepository.GetByIdAsync(id);

        if (existing is null)
            return null;

        if (!isAdmin)
        {
            var ownsRestaurant = await UserOwnsMenuItemRestaurantAsync(userId, existing.MenuId);
            if (!ownsRestaurant)
                return null;
        }

        existing.Name = dto.Name;
        existing.Description = dto.Description;
        existing.Price = dto.Price;
        existing.ImageUrl = dto.ImageUrl;
        existing.IsAvailable = dto.IsAvailable;
        existing.CategoryId = dto.CategoryId;

        // Kun admin må flytte en ret til en anden menu; restaurant-brugere er låst til deres egen.
        if (isAdmin)
            existing.MenuId = dto.MenuId;

        var updated = await _menuItemRepository.UpdateAsync(existing);
        return updated.ToDto();
    }

    /// <summary>
    /// Skifter billedet på en ret ud med et nyt, hvis brugeren ejer restauranten eller er admin.
    /// Det gamle billede bliver slettet fra disken, lige efter det nye er gemt, så vi ikke
    /// samler op på gamle, ubrugte filer.
    /// </summary>
    /// <returns>Den opdaterede ret som DTO, eller null hvis den ikke findes eller brugeren ikke må.</returns>
    public async Task<MenuItemDto?> UpdateImageUrlAsync(int id, string imageUrl, int userId, bool isAdmin)
    {
        var existing = await _menuItemRepository.GetByIdAsync(id);

        if (existing is null)
            return null;

        if (!isAdmin)
        {
            var ownsRestaurant = await UserOwnsMenuItemRestaurantAsync(userId, existing.MenuId);
            if (!ownsRestaurant)
                return null;
        }

        var previousImageUrl = existing.ImageUrl;
        existing.ImageUrl = imageUrl;
        var updated = await _menuItemRepository.UpdateAsync(existing);
        _imageStorageService.Delete(previousImageUrl);
        return updated.ToDto();
    }

    /// <summary>
    /// Sletter en ret (soft delete), men kun hvis brugeren ejer restauranten bag den, eller
    /// er admin. Retten bliver bare skjult, ikke rigtigt slettet, så den kan gendannes senere.
    /// </summary>
    /// <returns>True hvis den blev slettet, false hvis den ikke findes eller brugeren ikke må.</returns>
    public async Task<bool> DeleteAsync(int id, int userId, bool isAdmin)
    {
        var existing = await _menuItemRepository.GetByIdAsync(id);

        if (existing is null)
            return false;

        if (!isAdmin)
        {
            var ownsRestaurant = await UserOwnsMenuItemRestaurantAsync(userId, existing.MenuId);
            if (!ownsRestaurant)
                return false;
        }

        await _menuItemRepository.SoftDeleteAsync(existing);
        return true;
    }

    /// <summary>
    /// Gendanner en ret, der tidligere er blevet slettet, så den kommer tilbage til live,
    /// men kun hvis brugeren ejer restauranten eller er admin.
    /// </summary>
    /// <returns>True hvis den blev gendannet, false hvis den ikke findes, ikke var slettet, eller brugeren ikke må.</returns>
    public async Task<bool> RestoreAsync(int id, int userId, bool isAdmin)
    {
        var existing = await _menuItemRepository.GetByIdIncludingDeletedAsync(id);

        if (existing is null || !existing.IsDeleted)
            return false;

        if (!isAdmin)
        {
            var ownsRestaurant = await UserOwnsMenuItemRestaurantAsync(userId, existing.MenuId);
            if (!ownsRestaurant)
                return false;
        }

        await _menuItemRepository.RestoreAsync(existing);
        return true;
    }

    /// <summary>
    /// Sletter en ret FOR ALTID, uanset om den var soft-deleted eller ej, og kun hvis
    /// brugeren ejer restauranten eller er admin. Der er ingen fortryd-knap efter dette.
    /// </summary>
    /// <returns>True hvis den blev slettet permanent, false hvis den ikke findes eller brugeren ikke må.</returns>
    public async Task<bool> HardDeleteAsync(int id, int userId, bool isAdmin)
    {
        var existing = await _menuItemRepository.GetByIdIncludingDeletedAsync(id);

        if (existing is null)
            return false;

        if (!isAdmin)
        {
            var ownsRestaurant = await UserOwnsMenuItemRestaurantAsync(userId, existing.MenuId);
            if (!ownsRestaurant)
                return false;
        }

        await _menuItemRepository.HardDeleteAsync(existing);
        return true;
    }

    /// <summary>
    /// Finder ud af hvilken restaurant en rets menu hører under, og tjekker om den givne
    /// bruger ejer netop den restaurant. Bruger opslaget "også slettede", så ejerskab stadig
    /// kan tjekkes selvom menuen er blevet soft-deleted.
    /// </summary>
    /// <returns>True hvis brugeren ejer restauranten bag menuen.</returns>
    private async Task<bool> UserOwnsMenuItemRestaurantAsync(int userId, int menuId)
    {
        var menu = await _menuService.GetByIdIncludingDeletedAsync(menuId);
        return menu is not null && await UserOwnsRestaurantAsync(userId, menu.RestaurantId);
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
