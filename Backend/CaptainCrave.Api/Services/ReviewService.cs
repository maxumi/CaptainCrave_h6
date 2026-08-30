using Api.DTOs;
using Api.Models;
using Api.Repositories;

namespace Api.Services;

public class ReviewService : IReviewService
{
    private readonly IReviewRepository _reviewRepository;

    public ReviewService(IReviewRepository reviewRepository)
    {
        _reviewRepository = reviewRepository;
    }

    public async Task<RestaurantReviewSummaryDto> GetByRestaurantIdAsync(int restaurantId)
    {
        var reviews = await _reviewRepository.GetByRestaurantIdAsync(restaurantId);

        return new RestaurantReviewSummaryDto
        {
            RestaurantId = restaurantId,
            AverageRating = reviews.Count == 0
                ? 0
                : Math.Round(reviews.Average(r => r.Rating), 2),
            ReviewCount = reviews.Count,
            Reviews = reviews
                .Select(MapToDto)
                .ToList()
        };
    }

    public async Task<ReviewDto?> CreateAsync(int userId, CreateReviewDto dto)
    {
        if (dto.Rating < 1 || dto.Rating > 5)
        {
            return null;
        }

        var existingReview =
            await _reviewRepository.GetByUserAndRestaurantAsync(
                userId,
                dto.RestaurantId);

        if (existingReview != null)
        {
            return null;
        }

        var review = new Review
        {
            UserId = userId,
            RestaurantId = dto.RestaurantId,
            Rating = dto.Rating,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        var createdReview =
            await _reviewRepository.CreateAsync(review);

        return MapToDto(createdReview);
    }

    public async Task<ReviewDto?> UpdateAsync(
        int userId,
        int reviewId,
        UpdateReviewDto dto)
    {
        if (dto.Rating < 1 || dto.Rating > 5)
        {
            return null;
        }

        var review =
            await _reviewRepository.GetByIdAsync(reviewId);

        if (review == null)
        {
            return null;
        }

        if (review.UserId != userId)
        {
            return null;
        }

        review.Rating = dto.Rating;
        review.UpdatedAt = DateTime.UtcNow;

        var updatedReview =
            await _reviewRepository.UpdateAsync(review);

        return MapToDto(updatedReview);
    }

    private static ReviewDto MapToDto(Review review)
    {
        return new ReviewDto
        {
            Id = review.Id,
            UserId = review.UserId,
            RestaurantId = review.RestaurantId,
            Rating = review.Rating,
            CreatedAt = review.CreatedAt,
            UpdatedAt = review.UpdatedAt
        };
    }
}