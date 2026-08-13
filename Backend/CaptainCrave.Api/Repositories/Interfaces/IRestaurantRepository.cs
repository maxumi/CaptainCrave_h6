using Api.Models;

namespace Api.Repositories;

// Defines data access operations for restaurants.
public interface IRestaurantRepository
{
    // Returns all restaurants.
    Task<IEnumerable<Restaurant>> GetAllAsync();

    // Returns a restaurant by ID, or null if not found.
    Task<Restaurant?> GetByIdAsync(int id);

    // Returns all restaurants owned by the specified user.
    Task<IEnumerable<Restaurant>> GetByUserIdAsync(int userId);

    // Returns one restaurant owned by the specified user, or null if not found.
    Task<Restaurant?> GetSingleByUserIdAsync(int userId);

    // Saves a new restaurant and returns it with the generated ID.
    Task<Restaurant> CreateAsync(Restaurant restaurant);
}
