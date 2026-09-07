using Api.DTOs;

namespace Api.Services;

// Definerer forretningslogik for restauranter.
public interface IRestaurantService
{
    // Henter alle tilgængelige restauranter som DTO'er.
    Task<IEnumerable<RestaurantDto>> GetAllAsync();

    // Henter en restaurant ud fra ID, eller null hvis den ikke findes.
    Task<RestaurantDto?> GetByIdAsync(int id);

    // Henter restauranten der tilhører den angivne bruger, eller null hvis ingen findes.
    Task<RestaurantDto?> GetByUserIdAsync(int userId);

    // Validerer og opretter en ny restaurant og returnerer den som DTO.
    Task<RestaurantDto> CreateAsync(CreateRestaurantDto dto);

    // Opdaterer restaurantens profil, hvis brugeren ejer restauranten eller er administrator.
    Task<RestaurantDto?> UpdateAsync(int id, UpdateRestaurantDto dto, int userId, bool isAdmin);

    // Opdaterer restaurantens billed-URL, hvis brugeren ejer restauranten eller er administrator.
    Task<RestaurantDto?> UpdateImageUrlAsync(int id, string imageUrl, int userId, bool isAdmin);

    // Henter restauranter inden for den angivne radius fra en geografisk position.
    Task<IEnumerable<RestaurantDto>> GetNearbyRestaurantsAsync(
    double latitude,
    double longitude,
    double radiusKm);

    // Soft-deleter en restaurant, hvis brugeren ejer den eller er administrator.
    Task<bool> DeleteAsync(int id, int userId, bool isAdmin);

    // Gendanner en soft-deleted restaurant, hvis brugeren ejer den eller er administrator.
    Task<bool> RestoreAsync(int id, int userId, bool isAdmin);

    // Sletter en restaurant permanent, hvis brugeren ejer den eller er administrator.
    Task<bool> HardDeleteAsync(int id, int userId, bool isAdmin);

    // Henter alle soft-deleted restauranter til administratorens oversigt.
    Task<IEnumerable<RestaurantDto>> GetDeletedAsync();
}
