using Api.Models;

namespace Api.Repositories;

// Defines data access operations for menu items.
public interface IMenuItemRepository
{
    // Returns all menu items belonging to the specified restaurant.
    Task<IEnumerable<MenuItem>> GetByRestaurantIdAsync(int restaurantId);

    // Returns all menu items under the specified category.
    Task<IEnumerable<MenuItem>> GetByCategoryIdAsync(int categoryId);

    // Returns a menu item by ID, or null if not found.
    Task<MenuItem?> GetByIdAsync(int id);

    // Saves a new menu item and returns it with the generated ID.
    Task<MenuItem> CreateAsync(MenuItem menuItem);

    // Updates an existing menu item and returns the updated entity.
    Task<MenuItem> UpdateAsync(MenuItem menuItem);

    // Deletes an existing menu item.
    Task DeleteAsync(MenuItem menuItem);
}
