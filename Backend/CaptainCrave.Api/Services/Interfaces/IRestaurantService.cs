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

    Task<IEnumerable<RestaurantDto>> GetNearbyRestaurantsAsync(
    double latitude,
    double longitude,
    double radiusKm);
}
