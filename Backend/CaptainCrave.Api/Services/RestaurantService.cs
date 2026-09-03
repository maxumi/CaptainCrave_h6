using Api.DTOs;
using Api.Mappers;
using Api.Models;
using Api.Repositories;

namespace Api.Services;

// Handles business logic for restaurant operations.
public class RestaurantService(
    IRestaurantRepository restaurantRepository,
    IImageStorageService imageStorageService,
    IReviewRepository reviewRepository) : IRestaurantService
{
    private readonly IRestaurantRepository _restaurantRepository = restaurantRepository;
    private readonly IImageStorageService _imageStorageService = imageStorageService;
    private readonly IReviewRepository _reviewRepository = reviewRepository;

    // Retrieves all restaurants and maps them to DTOs, attaching each one's rating summary in a single bulk query.
    public async Task<IEnumerable<RestaurantDto>> GetAllAsync()
    {
        var restaurants = (await _restaurantRepository.GetAllAsync()).ToList();
        var summaries = await _reviewRepository.GetSummariesByRestaurantIdsAsync(restaurants.Select(r => r.Id));
        return restaurants.Select(r => ToDtoWithRating(r, summaries));
    }

    // Loads all restaurants and filters in-memory to those within radiusKm using the Haversine formula.
    public async Task<IEnumerable<RestaurantDto>> GetNearbyRestaurantsAsync(
        double latitude,
        double longitude,
        double radiusKm)
    {
        var restaurants = await _restaurantRepository.GetAllAsync();

        var nearby = restaurants
            .Where(r =>
                GetDistance(
                    latitude,
                    longitude,
                    r.Latitude,
                    r.Longitude) <= radiusKm)
            .ToList();

        var summaries = await _reviewRepository.GetSummariesByRestaurantIdsAsync(nearby.Select(r => r.Id));
        return nearby.Select(r => ToDtoWithRating(r, summaries));
    }

    // Retrieves a restaurant by ID and maps it to a DTO, including its rating summary.
    public async Task<RestaurantDto?> GetByIdAsync(int id)
    {
        var restaurant = await _restaurantRepository.GetByIdAsync(id);
        if (restaurant is null)
            return null;

        var (averageRating, reviewCount) = await _reviewRepository.GetSummaryAsync(id);
        return restaurant.ToDto(averageRating, reviewCount);
    }

    // Retrieves one restaurant by owner user ID and maps it to a DTO, including its rating summary.
    public async Task<RestaurantDto?> GetByUserIdAsync(int userId)
    {
        var restaurant = await _restaurantRepository.GetSingleByUserIdAsync(userId);
        if (restaurant is null)
            return null;

        var (averageRating, reviewCount) = await _reviewRepository.GetSummaryAsync(restaurant.Id);
        return restaurant.ToDto(averageRating, reviewCount);
    }

    // Maps the DTO to a model, saves it, and returns the created restaurant as a DTO.
    // A brand-new restaurant has no reviews yet, so no rating lookup is needed.
    public async Task<RestaurantDto> CreateAsync(CreateRestaurantDto dto)
    {
        var restaurant = dto.ToRestaurant();
        var created = await _restaurantRepository.CreateAsync(restaurant);
        return created.ToDto();
    }

    // Updates the editable restaurant fields when the caller owns it or is an admin.
    public async Task<RestaurantDto?> UpdateAsync(int id, UpdateRestaurantDto dto, int userId, bool isAdmin)
    {
        var restaurant = await _restaurantRepository.GetByIdAsync(id);
        if (restaurant is null)
            return null;

        if (!isAdmin && restaurant.UserId != userId)
            return null;

        restaurant.Name = dto.Name;
        restaurant.Description = dto.Description;
        restaurant.Address = dto.Address;
        restaurant.Latitude = dto.Latitude;
        restaurant.Longitude = dto.Longitude;
        restaurant.IsActive = dto.IsActive;

        var updated = await _restaurantRepository.UpdateAsync(restaurant);
        var (averageRating, reviewCount) = await _reviewRepository.GetSummaryAsync(id);
        return updated.ToDto(averageRating, reviewCount);
    }

    // Updates a restaurant's image URL when the caller owns it or is an admin, returning the updated DTO.
    public async Task<RestaurantDto?> UpdateImageUrlAsync(int id, string imageUrl, int userId, bool isAdmin)
    {
        var restaurant = await _restaurantRepository.GetByIdAsync(id);
        if (restaurant is null)
            return null;

        if (!isAdmin && restaurant.UserId != userId)
            return null;

        var previousImageUrl = restaurant.ImageUrl;
        restaurant.ImageUrl = imageUrl;
        var updated = await _restaurantRepository.UpdateAsync(restaurant);
        _imageStorageService.Delete(previousImageUrl);
        var (averageRating, reviewCount) = await _reviewRepository.GetSummaryAsync(id);
        return updated.ToDto(averageRating, reviewCount);
    }

    // Soft deletes a restaurant when the caller owns it or is an admin.
    public async Task<bool> DeleteAsync(int id, int userId, bool isAdmin)
    {
        var restaurant = await _restaurantRepository.GetByIdAsync(id);
        if (restaurant is null)
            return false;

        if (!isAdmin && restaurant.UserId != userId)
            return false;

        await _restaurantRepository.SoftDeleteAsync(restaurant);
        return true;
    }

    // Restores a previously soft-deleted restaurant when the caller owns it or is an admin.
    public async Task<bool> RestoreAsync(int id, int userId, bool isAdmin)
    {
        var restaurant = await _restaurantRepository.GetByIdIncludingDeletedAsync(id);
        if (restaurant is null || !restaurant.IsDeleted)
            return false;

        if (!isAdmin && restaurant.UserId != userId)
            return false;

        await _restaurantRepository.RestoreAsync(restaurant);
        return true;
    }

    // Permanently deletes a restaurant, soft-deleted or not, when the caller owns it or is an admin.
    public async Task<bool> HardDeleteAsync(int id, int userId, bool isAdmin)
    {
        var restaurant = await _restaurantRepository.GetByIdIncludingDeletedAsync(id);
        if (restaurant is null)
            return false;

        if (!isAdmin && restaurant.UserId != userId)
            return false;

        await _restaurantRepository.HardDeleteAsync(restaurant);
        return true;
    }

    // Returns every soft-deleted restaurant, for an admin trash view.
    public async Task<IEnumerable<RestaurantDto>> GetDeletedAsync()
    {
        var restaurants = (await _restaurantRepository.GetDeletedAsync()).ToList();
        var summaries = await _reviewRepository.GetSummariesByRestaurantIdsAsync(restaurants.Select(r => r.Id));
        return restaurants.Select(r => ToDtoWithRating(r, summaries));
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

    // Looks up a restaurant's precomputed rating summary (falling back to 0/0 if it has no reviews).
    private static RestaurantDto ToDtoWithRating(
        Restaurant restaurant,
        Dictionary<int, (double AverageRating, int ReviewCount)> summaries)
    {
        var (averageRating, reviewCount) = summaries.TryGetValue(restaurant.Id, out var summary)
            ? summary
            : (0, 0);

        return restaurant.ToDto(averageRating, reviewCount);
    }
}
