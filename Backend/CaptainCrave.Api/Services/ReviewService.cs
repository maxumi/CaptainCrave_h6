using Api.DTOs;
using Api.Models;
using Api.Repositories;

namespace Api.Services;

// Håndterer forretningslogikken for anmeldelser (reviews) og vurderinger.
public class ReviewService : IReviewService
{
    private readonly IReviewRepository _reviewRepository;
    private readonly IOrderRepository _orderRepository;

    // Opretter servicen med adgang til anmeldelser og ordrer. Ordre-adgangen bruges til
    // at bevise, at en kunde har handlet hos restauranten, før kunden må anmelde den.
    public ReviewService(IReviewRepository reviewRepository, IOrderRepository orderRepository)
    {
        _reviewRepository = reviewRepository;
        _orderRepository = orderRepository;
    }

    /// <summary>
    /// Henter en restaurants samlede vurdering: gennemsnitlig stjernescore og hvor mange
    /// anmeldelser den har fået i alt.
    /// </summary>
    /// <returns>En sammenfatning med gennemsnit og antal anmeldelser.</returns>
    public async Task<RestaurantReviewSummaryDto> GetByRestaurantIdAsync(int restaurantId)
    {
        var (average, count) = await _reviewRepository.GetSummaryAsync(restaurantId);

        return new RestaurantReviewSummaryDto
        {
            RestaurantId = restaurantId,
            AverageRating = average,
            ReviewCount = count
        };
    }

    /// <summary>
    /// Henter den anmeldelse, en bestemt bruger selv har skrevet om en restaurant, hvis der
    /// er skrevet en. Bruges så en bruger kan se og redigere sin egen anmeldelse.
    /// </summary>
    /// <returns>Brugerens egen anmeldelse som DTO, eller null hvis brugeren ikke har skrevet en.</returns>
    public async Task<ReviewDto?> GetMyReviewAsync(int userId, int restaurantId)
    {
        var review = await _reviewRepository.GetByUserAndRestaurantAsync(userId, restaurantId);
        return review is null ? null : MapToDto(review);
    }

    /// <summary>
    /// Opretter en ny anmeldelse. Der er tre regler, der skal være opfyldt: vurderingen skal
    /// være mellem 1 og 5 stjerner, brugeren må ikke allerede have anmeldt restauranten før,
    /// og brugeren skal rent faktisk have fået en ordre leveret fra restauranten.
    /// </summary>
    /// <returns>Den nye anmeldelse som DTO, eller null hvis vurderingen er ugyldig eller allerede findes.</returns>
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

        var hasOrdered =
            await _orderRepository.HasUserOrderedFromRestaurantAsync(userId, dto.RestaurantId);

        if (!hasOrdered)
        {
            throw new UnauthorizedAccessException(
                "You can only review a restaurant after a delivered order from it.");
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

    /// <summary>
    /// Opdaterer stjerne-vurderingen på en anmeldelse, som allerede findes, men kun hvis det
    /// er brugerens egen anmeldelse (man kan ikke redigere andres anmeldelser).
    /// </summary>
    /// <returns>Den opdaterede anmeldelse som DTO, eller null hvis vurderingen er ugyldig,
    /// anmeldelsen ikke findes, eller den ikke tilhører brugeren.</returns>
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

    /// <summary>
    /// Pakker en Review-model om til den DTO, der bliver sendt videre til klienten.
    /// </summary>
    /// <returns>Anmeldelsen som ReviewDto.</returns>
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
