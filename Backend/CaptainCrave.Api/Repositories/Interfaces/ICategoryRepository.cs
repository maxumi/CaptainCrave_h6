using Api.Models;

namespace Api.Repositories;

// Defines data access operations for categories.
public interface ICategoryRepository
{
    // Returns all categories belonging to the specified restaurant, across all of its menus.
    Task<IEnumerable<Category>> GetByRestaurantIdAsync(int restaurantId);

    // Returns all categories belonging to the specified menu.
    Task<IEnumerable<Category>> GetByMenuIdAsync(int menuId);

    // Returns a category by ID, or null if not found.
    Task<Category?> GetByIdAsync(int id);

    // Saves a new category and returns it with the generated ID.
    Task<Category> CreateAsync(Category category);
}
