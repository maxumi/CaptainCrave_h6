using Api.DTOs;

namespace Api.Services;

public interface IReviewService
{
    Task<RestaurantReviewSummaryDto> GetByRestaurantIdAsync(int restaurantId);

    Task<ReviewDto?> CreateAsync(int userId, CreateReviewDto dto);

    Task<ReviewDto?> UpdateAsync(int userId, int reviewId, UpdateReviewDto dto);
}