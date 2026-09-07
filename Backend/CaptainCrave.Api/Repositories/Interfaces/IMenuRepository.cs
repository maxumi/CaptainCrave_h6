using Api.Models;

namespace Api.Repositories;

// Definerer databaseoperationer for menuer.
public interface IMenuRepository
{
    /// <summary>
    /// Henter alle menuer der tilhører den angivne restaurant.
    /// </summary>
    /// <param name="restaurantId">ID på restauranten der ejer menuerne.</param>
    Task<IEnumerable<Menu>> GetByRestaurantIdAsync(int restaurantId);

    /// <summary>
    /// Henter en menu ud fra ID. Soft-deleted menuer medtages ikke.
    /// </summary>
    /// <param name="id">Menuens ID.</param>
    Task<Menu?> GetByIdAsync(int id);

    /// <summary>
    /// Henter en menu ud fra ID, også hvis den er soft-deleted.
    /// </summary>
    /// <param name="id">Menuens ID.</param>
    Task<Menu?> GetByIdIncludingDeletedAsync(int id);

    /// <summary>
    /// Henter soft-deleted menuer for en restaurant, så de kan vises og gendannes.
    /// </summary>
    /// <param name="restaurantId">ID på restauranten der ejer menuerne.</param>
    Task<IEnumerable<Menu>> GetDeletedByRestaurantIdAsync(int restaurantId);

    /// <summary>
    /// Gemmer en ny menu og returnerer den med det genererede ID.
    /// </summary>
    /// <param name="menu">Menuen der skal oprettes.</param>
    Task<Menu> CreateAsync(Menu menu);

    /// <summary>
    /// Soft-deleter en menu og dens menu-items uden at fjerne rækkerne permanent.
    /// </summary>
    /// <param name="menu">Menuen der skal soft-deletes.</param>
    Task SoftDeleteAsync(Menu menu);

    /// <summary>
    /// Gendanner en soft-deleted menu og dens soft-deleted menu-items.
    /// </summary>
    /// <param name="menu">Menuen der skal gendannes.</param>
    Task RestoreAsync(Menu menu);

    /// <summary>
    /// Sletter en menu permanent. Databasen cascade-sletter de tilhørende menu-items.
    /// </summary>
    /// <param name="menu">Menuen der skal slettes permanent.</param>
    Task HardDeleteAsync(Menu menu);
}
