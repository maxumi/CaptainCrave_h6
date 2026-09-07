using Api.DTOs;

namespace Api.Services;

// Definerer forretningslogik for restaurantanmeldelser.
public interface IReviewService
{
    // Henter restaurantens gennemsnitlige rating og antal anmeldelser.
    Task<RestaurantReviewSummaryDto> GetByRestaurantIdAsync(int restaurantId);

    // Henter brugerens egen anmeldelse af restauranten,
    // eller null hvis brugeren endnu ikke har anmeldt den.
    Task<ReviewDto?> GetMyReviewAsync(int userId, int restaurantId);

    // Opretter en anmeldelse, hvis brugeren opfylder kravene for at anmelde restauranten.
    Task<ReviewDto?> CreateAsync(int userId, CreateReviewDto dto);

    // Opdaterer brugerens eksisterende anmeldelse.
    Task<ReviewDto?> UpdateAsync(int userId, int reviewId, UpdateReviewDto dto);
}