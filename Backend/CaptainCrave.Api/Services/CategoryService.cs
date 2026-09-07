using Api.DTOs;
using Api.Mappers;
using Api.Repositories;

namespace Api.Services;

// Håndterer forretningslogikken for kategori-operationer.
public class CategoryService(ICategoryRepository categoryRepository, IMenuService menuService, IRestaurantRepository restaurantRepository) : ICategoryService
{
    private readonly ICategoryRepository _categoryRepository = categoryRepository;
    private readonly IMenuService _menuService = menuService;
    private readonly IRestaurantRepository _restaurantRepository = restaurantRepository;

    /// <summary>
    /// Henter alle kategorier, der hører til en restaurant, og pakker dem om til DTO'er.
    /// </summary>
    /// <returns>Alle kategorier for restauranten (kan være en tom liste).</returns>
    public async Task<IEnumerable<CategoryDto>> GetByRestaurantIdAsync(int restaurantId)
    {
        var categories = await _categoryRepository.GetByRestaurantIdAsync(restaurantId);
        return categories.Select(c => c.ToDto());
    }

    /// <summary>
    /// Henter alle kategorier, der hører til én bestemt menu, og pakker dem om til DTO'er.
    /// </summary>
    /// <returns>Alle kategorier for menuen (kan være en tom liste).</returns>
    public async Task<IEnumerable<CategoryDto>> GetByMenuIdAsync(int menuId)
    {
        var categories = await _categoryRepository.GetByMenuIdAsync(menuId);
        return categories.Select(c => c.ToDto());
    }

    /// <summary>
    /// Henter de kategorier, der er blevet slettet (soft delete, dvs. lagt i "papirkurven")
    /// for en restaurant, men kun hvis brugeren har lov til at se dem.
    /// </summary>
    /// <returns>De slettede kategorier, eller null hvis brugeren ikke må se dem.</returns>
    public async Task<IEnumerable<CategoryDto>?> GetDeletedByRestaurantIdAsync(int restaurantId, int userId, bool isAdmin)
    {
        if (!isAdmin && !await UserOwnsRestaurantAsync(userId, restaurantId))
            return null;

        var categories = await _categoryRepository.GetDeletedByRestaurantIdAsync(restaurantId);
        return categories.Select(c => c.ToDto());
    }

    /// <summary>
    /// Pakker DTO'en om til en rigtig Category-model, gemmer den i databasen,
    /// og giver den nye kategori tilbage som en DTO (nu med et rigtigt Id).
    /// </summary>
    /// <returns>Den nyoprettede kategori som DTO.</returns>
    public async Task<CategoryDto> CreateAsync(CreateCategoryDto dto)
    {
        var category = dto.ToCategory();
        var created = await _categoryRepository.CreateAsync(category);
        return created.ToDto();
    }

    /// <summary>
    /// Sletter en kategori (soft delete), men kun hvis brugeren ejer restauranten bag den,
    /// eller er admin. Kategorien bliver bare skjult, ikke rigtigt slettet, så den kan
    /// gendannes igen senere.
    /// </summary>
    /// <returns>True hvis den blev slettet, false hvis den ikke findes eller brugeren ikke må.</returns>
    public async Task<bool> DeleteAsync(int id, int userId, bool isAdmin)
    {
        var existing = await _categoryRepository.GetByIdAsync(id);

        if (existing is null)
            return false;

        if (!isAdmin && !await UserOwnsCategoryRestaurantAsync(userId, existing.MenuId))
            return false;

        await _categoryRepository.SoftDeleteAsync(existing);
        return true;
    }

    /// <summary>
    /// Gendanner en kategori, der tidligere er blevet slettet, så den kommer tilbage til live,
    /// men kun hvis brugeren ejer restauranten eller er admin.
    /// </summary>
    /// <returns>True hvis den blev gendannet, false hvis den ikke findes, ikke var slettet, eller brugeren ikke må.</returns>
    public async Task<bool> RestoreAsync(int id, int userId, bool isAdmin)
    {
        var existing = await _categoryRepository.GetByIdIncludingDeletedAsync(id);

        if (existing is null || !existing.IsDeleted)
            return false;

        if (!isAdmin && !await UserOwnsCategoryRestaurantAsync(userId, existing.MenuId))
            return false;

        await _categoryRepository.RestoreAsync(existing);
        return true;
    }

    /// <summary>
    /// Sletter en kategori FOR ALTID, uanset om den var soft-deleted eller ej, og kun hvis
    /// brugeren ejer restauranten eller er admin. Der er ingen fortryd-knap efter dette.
    /// </summary>
    /// <returns>True hvis den blev slettet permanent, false hvis den ikke findes eller brugeren ikke må.</returns>
    public async Task<bool> HardDeleteAsync(int id, int userId, bool isAdmin)
    {
        var existing = await _categoryRepository.GetByIdIncludingDeletedAsync(id);

        if (existing is null)
            return false;

        if (!isAdmin && !await UserOwnsCategoryRestaurantAsync(userId, existing.MenuId))
            return false;

        await _categoryRepository.HardDeleteAsync(existing);
        return true;
    }

    /// <summary>
    /// Finder ud af hvilken restaurant en kategoris menu hører under, og tjekker om den givne
    /// bruger ejer netop den restaurant. Bruger opslaget "også slettede", så ejerskab stadig
    /// kan tjekkes selvom menuen er blevet soft-deleted.
    /// </summary>
    /// <returns>True hvis brugeren ejer restauranten bag menuen.</returns>
    private async Task<bool> UserOwnsCategoryRestaurantAsync(int userId, int menuId)
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
