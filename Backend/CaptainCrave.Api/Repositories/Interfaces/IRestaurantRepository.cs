using Api.Models;

namespace Api.Repositories;

// Defines data access operations for restaurants.
public interface IRestaurantRepository
{
    // Returns all restaurants.
    Task<IEnumerable<Restaurant>> GetAllAsync();

    // Returns a restaurant by ID, or null if not found. Excludes soft-deleted restaurants.
    Task<Restaurant?> GetByIdAsync(int id);

    // Returns a restaurant by ID including soft-deleted ones, or null if it never existed.
    Task<Restaurant?> GetByIdIncludingDeletedAsync(int id);

    // Returns all soft-deleted restaurants, so they can be listed for restoring.
    Task<IEnumerable<Restaurant>> GetDeletedAsync();

    // Returns all restaurants owned by the specified user.
    Task<IEnumerable<Restaurant>> GetByUserIdAsync(int userId);

    // Returns one restaurant owned by the specified user, or null if not found.
    Task<Restaurant?> GetSingleByUserIdAsync(int userId);

    // Saves a new restaurant and returns it with the generated ID.
    Task<Restaurant> CreateAsync(Restaurant restaurant);

    // Marks a restaurant (and its menus and menu items) as deleted without removing the rows, so it can be restored later.
    Task SoftDeleteAsync(Restaurant restaurant);

    // Un-marks a soft-deleted restaurant (and its soft-deleted menus and menu items) so it appears again.
    Task RestoreAsync(Restaurant restaurant);

    // Permanently removes a restaurant and lets the database cascade delete its menus and menu items.
    Task HardDeleteAsync(Restaurant restaurant);
}
