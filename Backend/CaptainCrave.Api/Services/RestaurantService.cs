using Api.DTOs;
using Api.Mappers;
using Api.Repositories;

namespace Api.Services;

// Handles business logic for restaurant operations.
public class RestaurantService(IRestaurantRepository restaurantRepository) : IRestaurantService
{
    private readonly IRestaurantRepository _restaurantRepository = restaurantRepository;

    // Retrieves all restaurants and maps them to DTOs.
    public async Task<IEnumerable<RestaurantDto>> GetAllAsync()
    {
        var restaurants = await _restaurantRepository.GetAllAsync();
        return restaurants.Select(r => r.ToDto());
    }

    // Loads all restaurants and filters in-memory to those within radiusKm using the Haversine formula.
    public async Task<IEnumerable<RestaurantDto>> GetNearbyRestaurantsAsync(
        double latitude,
        double longitude,
        double radiusKm)
    {
        var restaurants = await _restaurantRepository.GetAllAsync();

        return restaurants
            .Where(r =>
                GetDistance(
                    latitude,
                    longitude,
                    r.Latitude,
                    r.Longitude) <= radiusKm)
            .Select(r => r.ToDto());
    }

    // Retrieves a restaurant by ID and maps it to a DTO.
    public async Task<RestaurantDto?> GetByIdAsync(int id)
    {
        var restaurant = await _restaurantRepository.GetByIdAsync(id);
        return restaurant?.ToDto();
    }

    // Retrieves one restaurant by owner user ID and maps it to a DTO.
    public async Task<RestaurantDto?> GetByUserIdAsync(int userId)
    {
        var restaurant = await _restaurantRepository.GetSingleByUserIdAsync(userId);
        return restaurant?.ToDto();
    }

    // Maps the DTO to a model, saves it, and returns the created restaurant as a DTO.
    public async Task<RestaurantDto> CreateAsync(CreateRestaurantDto dto)
    {
        var restaurant = dto.ToRestaurant();
        var created = await _restaurantRepository.CreateAsync(restaurant);
        return created.ToDto();
    }

    // Returns the great-circle distance in kilometres between two lat/lng points (Haversine formula, Earth radius = 6371 km).
    private static double GetDistance(
        double lat1,
        double lon1,
        double lat2,
        double lon2)
    {
        const double R = 6371;

        var dLat = DegreesToRadians(lat2 - lat1);
        var dLon = DegreesToRadians(lon2 - lon1);

        var a =
            Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
            Math.Cos(DegreesToRadians(lat1)) *
            Math.Cos(DegreesToRadians(lat2)) *
            Math.Sin(dLon / 2) *
            Math.Sin(dLon / 2);

        var c = 2 * Math.Atan2(
            Math.Sqrt(a),
            Math.Sqrt(1 - a));

        return R * c;
    }

    private static double DegreesToRadians(double degrees)
    {
        return degrees * Math.PI / 180;
    }
}
