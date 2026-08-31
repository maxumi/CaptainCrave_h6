using Api.DTOs;

namespace Api.Services;

public interface IReviewService
{
    Task<RestaurantReviewSummaryDto> GetByRestaurantIdAsync(int restaurantId);

    // Returns the logged-in user's own review for a restaurant, or null if they have not reviewed it.
    Task<ReviewDto?> GetMyReviewAsync(int userId, int restaurantId);

    Task<ReviewDto?> CreateAsync(int userId, CreateReviewDto dto);

    Task<ReviewDto?> UpdateAsync(int userId, int reviewId, UpdateReviewDto dto);
}