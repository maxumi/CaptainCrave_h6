using Api.Models;

namespace Api.Repositories;

// Definerer databaseoperationer for anmeldelser og restaurantvurderinger.
public interface IReviewRepository
{
    // Beregner gennemsnitlig rating og antal anmeldelser for én restaurant direkte i databasen.
    Task<(double AverageRating, int ReviewCount)> GetSummaryAsync(int restaurantId);

    // Beregner rating-oplysninger for flere restauranter i én databaseforespørgsel.
    // Resultatet indekseres efter restaurantens ID.
    Task<Dictionary<int, (double AverageRating, int ReviewCount)>> GetSummariesByRestaurantIdsAsync(IEnumerable<int> restaurantIds);

    // Henter en anmeldelse ud fra ID, eller null hvis den ikke findes.
    Task<Review?> GetByIdAsync(int id);

    // Henter brugerens anmeldelse af en bestemt restaurant, eller null hvis den ikke findes.
    Task<Review?> GetByUserAndRestaurantAsync(int userId, int restaurantId);

    // Gemmer en ny anmeldelse og returnerer den oprettede entity.
    Task<Review> CreateAsync(Review review);

    // Opdaterer en eksisterende anmeldelse og returnerer den opdaterede entity.
    Task<Review> UpdateAsync(Review review);
}