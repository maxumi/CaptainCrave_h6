using Api.Models;

namespace Api.Repositories;

// Defines data access operations for categories.
public interface ICategoryRepository
{
    // Returns all categories belonging to the specified restaurant.
    Task<IEnumerable<Category>> GetByRestaurantIdAsync(int restaurantId);

    // Returns a category by ID, or null if not found.
    Task<Category?> GetByIdAsync(int id);

    // Saves a new category and returns it with the generated ID.
    Task<Category> CreateAsync(Category category);
}
