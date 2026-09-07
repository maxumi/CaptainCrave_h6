using Api.DTOs;
using Api.Models.Enums;

namespace Api.Services;

// Definerer forretningslogik for ordrer.
public interface IOrderService
{
    // Henter en ordre ud fra ID, eller null hvis den ikke findes.
    Task<OrderDto?> GetByIdAsync(int id);

    // Validerer og opretter en ny ordre og returnerer den som DTO.
    Task<OrderDto> CreateAsync(CreateOrderDto dto);

    // Opdaterer status på en ordre.
    // Adgang og gyldige statusskift håndteres i service-laget.
    Task<bool> UpdateStatusAsync(int id, UpdateOrderStatusDto dto, int currentUserId, UserRole currentUserRole);

    // Henter aktive ordrer for restauranten der er tilknyttet den aktuelle bruger.
    Task<IEnumerable<OrderDto>> GetRestaurantActiveOrdersAsync(int currentUserId, UserRole currentUserRole);

    // Henter leverede og annullerede ordrer for restauranten der er tilknyttet den aktuelle bruger.
    Task<IEnumerable<OrderDto>> GetRestaurantHistoricOrdersAsync(int currentUserId, UserRole currentUserRole);

    // Henter aktive ordrer for en restaurant ud fra restaurantens ID.
    Task<IEnumerable<OrderDto>> GetRestaurantActiveOrdersByRestaurantIdAsync(int restaurantId);

    // Henter historiske ordrer for en restaurant ud fra restaurantens ID.
    Task<IEnumerable<OrderDto>> GetRestaurantHistoricOrdersByRestaurantIdAsync(int restaurantId);


    // Henter brugerens aktive ordre, eller null hvis der ikke findes en.
    Task<OrderDto?> GetActiveOrderForUserAsync(int userId);

    // Henter leverede og annullerede ordrer for en bruger.
    Task<IEnumerable<OrderDto>> GetHistoricOrdersForUserAsync(int userId);

    // Kontrollerer om brugeren har en leveret ordre fra restauranten.
    // Bruges til at afgøre om kunden må anmelde restauranten.
    Task<bool> HasCustomerOrderedFromRestaurantAsync(int userId, int restaurantId);
}