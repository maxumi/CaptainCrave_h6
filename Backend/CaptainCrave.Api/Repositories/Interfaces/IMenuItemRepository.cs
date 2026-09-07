using Api.Models;

namespace Api.Repositories;

// Definerer databaseoperationer for menu-items.
public interface IMenuItemRepository
{
    // Henter alle menu-items for en restaurant på tværs af restaurantens menuer.
    Task<IEnumerable<MenuItem>> GetByRestaurantIdAsync(int restaurantId);

    // Henter alle menu-items der tilhører den angivne menu.
    Task<IEnumerable<MenuItem>> GetByMenuIdAsync(int menuId);

    // Henter alle menu-items der tilhører den angivne kategori.
    Task<IEnumerable<MenuItem>> GetByCategoryIdAsync(int categoryId);

    // Henter et menu-item ud fra ID. Soft-deleted menu-items medtages ikke.
    Task<MenuItem?> GetByIdAsync(int id);

    // Henter et menu-item ud fra ID, også hvis det er soft-deleted.
    Task<MenuItem?> GetByIdIncludingDeletedAsync(int id);

    // Henter soft-deleted menu-items for en restaurant, så de kan vises og gendannes.
    Task<IEnumerable<MenuItem>> GetDeletedByRestaurantIdAsync(int restaurantId);

    // Gemmer et nyt menu-item og returnerer det med det genererede ID.
    Task<MenuItem> CreateAsync(MenuItem menuItem);

    // Opdaterer et eksisterende menu-item og returnerer den opdaterede entity.
    Task<MenuItem> UpdateAsync(MenuItem menuItem);

    // Soft-deleter et menu-item uden at fjerne rækken fra databasen.
    Task SoftDeleteAsync(MenuItem menuItem);

    // Gendanner et tidligere soft-deleted menu-item.
    Task RestoreAsync(MenuItem menuItem);

    // Sletter et menu-item permanent fra databasen.
    Task HardDeleteAsync(MenuItem menuItem);
}
