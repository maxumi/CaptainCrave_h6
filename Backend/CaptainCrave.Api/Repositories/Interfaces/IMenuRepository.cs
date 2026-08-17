using Api.Models;

namespace Api.Repositories;

// Defines data access operations for menus.
public interface IMenuRepository
{
    /// <summary>
    /// Returns all menus belonging to the specified restaurant.
    /// </summary>
    /// <param name="restaurantId">The owning restaurant's id.</param>
    Task<IEnumerable<Menu>> GetByRestaurantIdAsync(int restaurantId);

    /// <summary>
    /// Returns a menu by ID, or null if not found.
    /// </summary>
    /// <param name="id">The menu's id.</param>
    Task<Menu?> GetByIdAsync(int id);

    /// <summary>
    /// Saves a new menu and returns it with the generated ID.
    /// </summary>
    /// <param name="menu">The entity to insert.</param>
    Task<Menu> CreateAsync(Menu menu);

    /// <summary>
    /// Removes a menu and any child menu items through cascade delete.
    /// </summary>
    /// <param name="menu">The entity to delete.</param>
    Task DeleteAsync(Menu menu);
}
