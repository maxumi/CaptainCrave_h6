using Api.DTOs;
using Api.Mappers;
using Api.Models;
using Api.Repositories;

namespace Api.Services;

// Håndterer forretningslogikken for restaurant-operationer.
public class RestaurantService(
    IRestaurantRepository restaurantRepository,
    IImageStorageService imageStorageService,
    IReviewRepository reviewRepository) : IRestaurantService
{
    private readonly IRestaurantRepository _restaurantRepository = restaurantRepository;
    private readonly IImageStorageService _imageStorageService = imageStorageService;
    private readonly IReviewRepository _reviewRepository = reviewRepository;

    /// <summary>
    /// Henter alle restauranter og pakker dem om til DTO'er, med vurderinger hentet
    /// i ét samlet databaseopslag i stedet for et opslag pr. restaurant (meget hurtigere).
    /// </summary>
    /// <returns>Alle restauranter med gennemsnitsvurdering og antal anmeldelser påsat.</returns>
    public async Task<IEnumerable<RestaurantDto>> GetAllAsync()
    {
        var restaurants = (await _restaurantRepository.GetAllAsync()).ToList();
        var summaries = await _reviewRepository.GetSummariesByRestaurantIdsAsync(restaurants.Select(r => r.Id));
        return restaurants.Select(r => ToDtoWithRating(r, summaries));
    }

    /// <summary>
    /// Henter alle restauranter og behold kun dem, der ligger inden for en bestemt radius (km)
    /// fra et punkt på kortet. Afstanden regnes ud med Haversine-formlen, som tager højde for
    /// at jorden er en kugle og ikke flad.
    /// </summary>
    /// <returns>De restauranter, der ligger inden for radius, med vurdering påsat.</returns>
    public async Task<IEnumerable<RestaurantDto>> GetNearbyRestaurantsAsync(
        double latitude,
        double longitude,
        double radiusKm)
    {
        var restaurants = await _restaurantRepository.GetAllAsync();

        var nearby = restaurants
            .Where(r =>
                GetDistance(
                    latitude,
                    longitude,
                    r.Latitude,
                    r.Longitude) <= radiusKm)
            .ToList();

        var summaries = await _reviewRepository.GetSummariesByRestaurantIdsAsync(nearby.Select(r => r.Id));
        return nearby.Select(r => ToDtoWithRating(r, summaries));
    }

    /// <summary>
    /// Henter én restaurant ud fra dens id, sammen med dens vurdering (gennemsnit og antal anmeldelser).
    /// </summary>
    /// <returns>Restauranten som DTO, eller null hvis den ikke findes.</returns>
    public async Task<RestaurantDto?> GetByIdAsync(int id)
    {
        var restaurant = await _restaurantRepository.GetByIdAsync(id);
        if (restaurant is null)
            return null;

        var (averageRating, reviewCount) = await _reviewRepository.GetSummaryAsync(id);
        return restaurant.ToDto(averageRating, reviewCount);
    }

    /// <summary>
    /// Henter den ene restaurant, som en bestemt bruger ejer, sammen med dens vurdering.
    /// </summary>
    /// <returns>Restauranten som DTO, eller null hvis brugeren ikke ejer nogen restaurant.</returns>
    public async Task<RestaurantDto?> GetByUserIdAsync(int userId)
    {
        var restaurant = await _restaurantRepository.GetSingleByUserIdAsync(userId);
        if (restaurant is null)
            return null;

        var (averageRating, reviewCount) = await _reviewRepository.GetSummaryAsync(restaurant.Id);
        return restaurant.ToDto(averageRating, reviewCount);
    }

    /// <summary>
    /// Pakker DTO'en om til en rigtig Restaurant-model og gemmer den i databasen.
    /// En helt ny restaurant har ingen anmeldelser endnu, så der bliver ikke slået
    /// vurderinger op her, de starter på 0.
    /// </summary>
    /// <returns>Den nyoprettede restaurant som DTO.</returns>
    public async Task<RestaurantDto> CreateAsync(CreateRestaurantDto dto)
    {
        var restaurant = dto.ToRestaurant();
        var created = await _restaurantRepository.CreateAsync(restaurant);
        return created.ToDto();
    }

    /// <summary>
    /// Opdaterer restaurantens redigerbare felter (navn, beskrivelse, adresse osv.), men kun
    /// hvis brugeren selv ejer restauranten eller er admin.
    /// </summary>
    /// <returns>Den opdaterede restaurant som DTO, eller null hvis den ikke findes eller brugeren ikke må.</returns>
    public async Task<RestaurantDto?> UpdateAsync(int id, UpdateRestaurantDto dto, int userId, bool isAdmin)
    {
        var restaurant = await _restaurantRepository.GetByIdAsync(id);
        if (restaurant is null)
            return null;

        if (!isAdmin && restaurant.UserId != userId)
            return null;

        restaurant.Name = dto.Name;
        restaurant.Description = dto.Description;
        restaurant.Address = dto.Address;
        restaurant.Latitude = dto.Latitude;
        restaurant.Longitude = dto.Longitude;
        restaurant.IsActive = dto.IsActive;

        var updated = await _restaurantRepository.UpdateAsync(restaurant);
        var (averageRating, reviewCount) = await _reviewRepository.GetSummaryAsync(id);
        return updated.ToDto(averageRating, reviewCount);
    }

    /// <summary>
    /// Skifter restaurantens billede ud med et nyt, hvis brugeren ejer den eller er admin.
    /// Det gamle billede bliver slettet fra disken, lige efter det nye er gemt.
    /// </summary>
    /// <returns>Den opdaterede restaurant som DTO, eller null hvis den ikke findes eller brugeren ikke må.</returns>
    public async Task<RestaurantDto?> UpdateImageUrlAsync(int id, string imageUrl, int userId, bool isAdmin)
    {
        var restaurant = await _restaurantRepository.GetByIdAsync(id);
        if (restaurant is null)
            return null;

        if (!isAdmin && restaurant.UserId != userId)
            return null;

        var previousImageUrl = restaurant.ImageUrl;
        restaurant.ImageUrl = imageUrl;
        var updated = await _restaurantRepository.UpdateAsync(restaurant);
        _imageStorageService.Delete(previousImageUrl);
        var (averageRating, reviewCount) = await _reviewRepository.GetSummaryAsync(id);
        return updated.ToDto(averageRating, reviewCount);
    }

    /// <summary>
    /// Sletter en restaurant (soft delete), men kun hvis brugeren ejer den eller er admin.
    /// Den bliver bare skjult, ikke rigtigt slettet, så den kan gendannes senere.
    /// </summary>
    /// <returns>True hvis den blev slettet, false hvis den ikke findes eller brugeren ikke må.</returns>
    public async Task<bool> DeleteAsync(int id, int userId, bool isAdmin)
    {
        var restaurant = await _restaurantRepository.GetByIdAsync(id);
        if (restaurant is null)
            return false;

        if (!isAdmin && restaurant.UserId != userId)
            return false;

        await _restaurantRepository.SoftDeleteAsync(restaurant);
        return true;
    }

    /// <summary>
    /// Gendanner en restaurant, der tidligere er blevet slettet, så den kommer tilbage til live,
    /// men kun hvis brugeren ejer den eller er admin.
    /// </summary>
    /// <returns>True hvis den blev gendannet, false hvis den ikke findes, ikke var slettet, eller brugeren ikke må.</returns>
    public async Task<bool> RestoreAsync(int id, int userId, bool isAdmin)
    {
        var restaurant = await _restaurantRepository.GetByIdIncludingDeletedAsync(id);
        if (restaurant is null || !restaurant.IsDeleted)
            return false;

        if (!isAdmin && restaurant.UserId != userId)
            return false;

        await _restaurantRepository.RestoreAsync(restaurant);
        return true;
    }

    /// <summary>
    /// Sletter en restaurant FOR ALTID, uanset om den var soft-deleted eller ej, og kun hvis
    /// brugeren ejer den eller er admin. Der er ingen fortryd-knap efter dette.
    /// </summary>
    /// <returns>True hvis den blev slettet permanent, false hvis den ikke findes eller brugeren ikke må.</returns>
    public async Task<bool> HardDeleteAsync(int id, int userId, bool isAdmin)
    {
        var restaurant = await _restaurantRepository.GetByIdIncludingDeletedAsync(id);
        if (restaurant is null)
            return false;

        if (!isAdmin && restaurant.UserId != userId)
            return false;

        await _restaurantRepository.HardDeleteAsync(restaurant);
        return true;
    }

    /// <summary>
    /// Henter alle slettede (soft delete) restauranter til admins "papirkurv"-visning.
    /// </summary>
    /// <returns>Alle slettede restauranter, med vurdering påsat.</returns>
    public async Task<IEnumerable<RestaurantDto>> GetDeletedAsync()
    {
        var restaurants = (await _restaurantRepository.GetDeletedAsync()).ToList();
        var summaries = await _reviewRepository.GetSummariesByRestaurantIdsAsync(restaurants.Select(r => r.Id));
        return restaurants.Select(r => ToDtoWithRating(r, summaries));
    }

    /// <summary>
    /// Regner afstanden i kilometer ud mellem to punkter på jorden (Haversine-formlen).
    /// Jordens radius er sat til 6371 km. Bruges til "restauranter i nærheden".
    /// </summary>
    /// <returns>Afstanden mellem punkterne i kilometer.</returns>
    private static double GetDistance(
        double lat1,
        double lon1,
        double lat2,
        double lon2)
    {
        const double R = 6371;

        var dLat = DegreesToRadians(lat2 - lat1);
        var dLon = DegreesToRadians(lon2 - lon1);

        var a =
            Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
            Math.Cos(DegreesToRadians(lat1)) *
            Math.Cos(DegreesToRadians(lat2)) *
            Math.Sin(dLon / 2) *
            Math.Sin(dLon / 2);

        var c = 2 * Math.Atan2(
            Math.Sqrt(a),
            Math.Sqrt(1 - a));

        return R * c;
    }

    /// <summary>
    /// Omregner en vinkel fra grader til radianer, som Math-funktionerne kræver.
    /// </summary>
    /// <returns>Den samme vinkel målt i radianer.</returns>
    private static double DegreesToRadians(double degrees)
    {
        return degrees * Math.PI / 180;
    }

    /// <summary>
    /// Slår en restaurants færdigberegnede vurdering op i en opslags-ordbog, eller
    /// falder tilbage til 0 stjerner/0 anmeldelser, hvis restauranten slet ingen anmeldelser har.
    /// </summary>
    /// <returns>Restauranten som DTO med gennemsnitsvurdering og antal anmeldelser.</returns>
    private static RestaurantDto ToDtoWithRating(
        Restaurant restaurant,
        Dictionary<int, (double AverageRating, int ReviewCount)> summaries)
    {
        var (averageRating, reviewCount) = summaries.TryGetValue(restaurant.Id, out var summary)
            ? summary
            : (0, 0);

        return restaurant.ToDto(averageRating, reviewCount);
    }
}
