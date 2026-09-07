using Api.Models;

namespace Api.Repositories;

// Definerer databaseoperationer for kategorier.
public interface ICategoryRepository
{
    // Henter alle kategorier for en restaurant på tværs af restaurantens menuer.
    Task<IEnumerable<Category>> GetByRestaurantIdAsync(int restaurantId);

    // Henter alle kategorier der tilhører den angivne menu.
    Task<IEnumerable<Category>> GetByMenuIdAsync(int menuId);

    // Henter en kategori ud fra ID. Soft-deleted kategorier medtages ikke.
    Task<Category?> GetByIdAsync(int id);

    // Henter en kategori ud fra ID, også hvis den er soft-deleted.
    Task<Category?> GetByIdIncludingDeletedAsync(int id);

    // Henter soft-deleted kategorier for en restaurant, så de kan vises og gendannes.
    Task<IEnumerable<Category>> GetDeletedByRestaurantIdAsync(int restaurantId);

    // Gemmer en ny kategori og returnerer den med det genererede ID.
    Task<Category> CreateAsync(Category category);

    // Soft-deleter en kategori uden at fjerne rækken fra databasen.
    Task SoftDeleteAsync(Category category);

    // Gendanner en tidligere soft-deleted kategori.
    Task RestoreAsync(Category category);

    // Sletter en kategori permanent fra databasen.
    Task HardDeleteAsync(Category category);
}
