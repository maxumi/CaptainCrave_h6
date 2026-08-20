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
    /// Returns a menu by ID, or null if not found. Excludes soft-deleted menus.
    /// </summary>
    /// <param name="id">The menu's id.</param>
    Task<Menu?> GetByIdAsync(int id);

    /// <summary>
    /// Returns a menu by ID including soft-deleted ones, or null if it never existed.
    /// </summary>
    /// <param name="id">The menu's id.</param>
    Task<Menu?> GetByIdIncludingDeletedAsync(int id);

    /// <summary>
    /// Returns the soft-deleted menus for a restaurant, so they can be listed for restoring.
    /// </summary>
    /// <param name="restaurantId">The owning restaurant's id.</param>
    Task<IEnumerable<Menu>> GetDeletedByRestaurantIdAsync(int restaurantId);

    /// <summary>
    /// Saves a new menu and returns it with the generated ID.
    /// </summary>
    /// <param name="menu">The entity to insert.</param>
    Task<Menu> CreateAsync(Menu menu);

    /// <summary>
    /// Marks a menu (and its menu items) as deleted without removing the rows, so it can be restored later.
    /// </summary>
    /// <param name="menu">The entity to soft delete.</param>
    Task SoftDeleteAsync(Menu menu);

    /// <summary>
    /// Un-marks a soft-deleted menu (and its soft-deleted menu items) so it appears again.
    /// </summary>
    /// <param name="menu">The entity to restore.</param>
    Task RestoreAsync(Menu menu);

    /// <summary>
    /// Permanently removes a menu and lets the database cascade delete its menu items.
    /// </summary>
    /// <param name="menu">The entity to delete.</param>
    Task HardDeleteAsync(Menu menu);
}
