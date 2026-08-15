using Api.Models;

namespace Api.Repositories;

// Defines data access operations for menus.
public interface IMenuRepository
{
    // Returns all menus belonging to the specified restaurant.
    Task<IEnumerable<Menu>> GetByRestaurantIdAsync(int restaurantId);

    // Returns a menu by ID, or null if not found.
    Task<Menu?> GetByIdAsync(int id);

    // Saves a new menu and returns it with the generated ID.
    Task<Menu> CreateAsync(Menu menu);
}
