using Api.Models;
using Api.Models.Enums;

namespace Api.Repositories;

// Definerer databaseoperationer for ordrer.
public interface IOrderRepository
{
    // Henter en ordre med dens relaterede data, eller null hvis den ikke findes.
    Task<Order?> GetByIdAsync(int id);

    // Gemmer en ny ordre (med dens items) og returnerer den med det genererede ID.
    Task<Order> CreateAsync(Order order);

    // Opdaterer status og updated_at på en eksisterende ordre.
    // Returnerer false hvis ordren ikke findes.
    Task<bool> UpdateStatusAsync(int id, OrderStatus status);

    // Henter aktive ordrer for en restaurant.
    Task<IEnumerable<Order>> GetActiveByRestaurantAsync(int restaurantId);

    // Henter leverede og annullerede ordrer for en restaurant.
    Task<IEnumerable<Order>> GetHistoryByRestaurantAsync(int restaurantId);

    // Henter brugerens første aktive ordre, eller null hvis der ikke findes en.
    Task<Order?> GetActiveOrderForUserAsync(int userId);

    // Henter leverede og annullerede ordrer for en bruger.
    Task<IEnumerable<Order>> GetHistoryForUserAsync(int userId);

    // Kontrollerer om brugeren har en leveret ordre fra restauranten.
    // Bruges til at afgøre om brugeren må anmelde restauranten.
    Task<bool> HasUserOrderedFromRestaurantAsync(int userId, int restaurantId);
}
