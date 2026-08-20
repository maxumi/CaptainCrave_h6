using Api.Models;

namespace Api.Repositories;

// Defines data access operations for categories.
public interface ICategoryRepository
{
    // Returns all categories belonging to the specified restaurant, across all of its menus.
    Task<IEnumerable<Category>> GetByRestaurantIdAsync(int restaurantId);

    // Returns all categories belonging to the specified menu.
    Task<IEnumerable<Category>> GetByMenuIdAsync(int menuId);

    // Returns a category by ID, or null if not found. Excludes soft-deleted categories.
    Task<Category?> GetByIdAsync(int id);

    // Returns a category by ID including soft-deleted ones, or null if it never existed.
    Task<Category?> GetByIdIncludingDeletedAsync(int id);

    // Returns the soft-deleted categories for a restaurant, so they can be listed for restoring.
    Task<IEnumerable<Category>> GetDeletedByRestaurantIdAsync(int restaurantId);

    // Saves a new category and returns it with the generated ID.
    Task<Category> CreateAsync(Category category);

    // Marks a category as deleted without removing the row, so it can be restored later.
    Task SoftDeleteAsync(Category category);

    // Un-marks a soft-deleted category so it appears again.
    Task RestoreAsync(Category category);

    // Permanently removes a category row.
    Task HardDeleteAsync(Category category);
}
