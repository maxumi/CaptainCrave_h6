using Api.Models;

namespace Api.Repositories;

// Defines data access operations for menu items.
public interface IMenuItemRepository
{
    // Returns all menu items belonging to the specified restaurant, across all of its menus.
    Task<IEnumerable<MenuItem>> GetByRestaurantIdAsync(int restaurantId);

    // Returns all menu items belonging to the specified menu.
    Task<IEnumerable<MenuItem>> GetByMenuIdAsync(int menuId);

    // Returns all menu items under the specified category.
    Task<IEnumerable<MenuItem>> GetByCategoryIdAsync(int categoryId);

    // Returns a menu item by ID, or null if not found. Excludes soft-deleted items.
    Task<MenuItem?> GetByIdAsync(int id);

    // Returns a menu item by ID including soft-deleted ones, or null if it never existed.
    Task<MenuItem?> GetByIdIncludingDeletedAsync(int id);

    // Returns the soft-deleted menu items for a restaurant, so they can be listed for restoring.
    Task<IEnumerable<MenuItem>> GetDeletedByRestaurantIdAsync(int restaurantId);

    // Saves a new menu item and returns it with the generated ID.
    Task<MenuItem> CreateAsync(MenuItem menuItem);

    // Updates an existing menu item and returns the updated entity.
    Task<MenuItem> UpdateAsync(MenuItem menuItem);

    // Marks a menu item as deleted without removing the row, so it can be restored later.
    Task SoftDeleteAsync(MenuItem menuItem);

    // Un-marks a soft-deleted menu item so it appears again.
    Task RestoreAsync(MenuItem menuItem);

    // Permanently removes a menu item row.
    Task HardDeleteAsync(MenuItem menuItem);
}
