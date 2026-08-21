using Api.DTOs;

namespace Api.Services;

// Defines business logic operations for restaurants.
public interface IRestaurantService
{
    // Returns all restaurants as DTOs.
    Task<IEnumerable<RestaurantDto>> GetAllAsync();

    // Returns a single restaurant DTO by ID, or null if not found.
    Task<RestaurantDto?> GetByIdAsync(int id);

    // Returns one restaurant DTO owned by the specified user, or null if not found.
    Task<RestaurantDto?> GetByUserIdAsync(int userId);

    // Validates, creates, and returns the new restaurant as a DTO.
    Task<RestaurantDto> CreateAsync(CreateRestaurantDto dto);

    // Updates a restaurant's profile when the caller owns it or is an admin.
    Task<RestaurantDto?> UpdateAsync(int id, UpdateRestaurantDto dto, int userId, bool isAdmin);

    // Updates a restaurant's image URL when the caller owns it or is an admin, returning the updated DTO.
    Task<RestaurantDto?> UpdateImageUrlAsync(int id, string imageUrl, int userId, bool isAdmin);

    Task<IEnumerable<RestaurantDto>> GetNearbyRestaurantsAsync(
    double latitude,
    double longitude,
    double radiusKm);

    // Soft deletes a restaurant when the caller owns it or is an admin. The restaurant can be restored later.
    Task<bool> DeleteAsync(int id, int userId, bool isAdmin);

    // Restores a soft-deleted restaurant when the caller owns it or is an admin.
    Task<bool> RestoreAsync(int id, int userId, bool isAdmin);

    // Permanently deletes a restaurant (soft-deleted or not) when the caller owns it or is an admin.
    Task<bool> HardDeleteAsync(int id, int userId, bool isAdmin);

    // Returns every soft-deleted restaurant, for an admin trash view.
    Task<IEnumerable<RestaurantDto>> GetDeletedAsync();
}
