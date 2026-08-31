using Api.Models;

namespace Api.Repositories;

public interface IReviewRepository
{
    // Average rating + count for one restaurant, computed via a SQL aggregate (no row loading).
    Task<(double AverageRating, int ReviewCount)> GetSummaryAsync(int restaurantId);

    // Average rating + count for many restaurants in a single query, keyed by restaurant ID.
    Task<Dictionary<int, (double AverageRating, int ReviewCount)>> GetSummariesByRestaurantIdsAsync(IEnumerable<int> restaurantIds);

    Task<Review?> GetByIdAsync(int id);

    Task<Review?> GetByUserAndRestaurantAsync(int userId, int restaurantId);

    Task<Review> CreateAsync(Review review);

    Task<Review> UpdateAsync(Review review);
}