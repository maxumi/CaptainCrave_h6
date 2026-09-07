using Api.DTOs;
using Api.Models;

namespace Api.Mappers;

// Denne klasse hjælper os med at bygge om mellem Restaurant (det vi gemmer i databasen)
// og de DTO'er vi sender frem og tilbage til klienten.
public static class RestaurantMapper
{
    /// <summary>
    /// Tager en restaurant og pakker den om til en RestaurantDto. Gennemsnitsvurdering og
    /// antal anmeldelser sendes med som separate parametre, fordi de bliver udregnet i en
    /// anden forespørgsel (restauranten selv har ikke anmeldelserne loadet ind).
    /// </summary>
    /// <returns>En RestaurantDto klar til at blive sendt til klienten.</returns>
    public static RestaurantDto ToDto(this Restaurant restaurant, double averageRating = 0, int reviewCount = 0) => new()
    {
        Id = restaurant.Id,
        UserId = restaurant.UserId,
        Name = restaurant.Name,
        Description = restaurant.Description,
        Address = restaurant.Address,
        Latitude = restaurant.Latitude,
        Longitude = restaurant.Longitude,
        ImageUrl = restaurant.ImageUrl,
        IsActive = restaurant.IsActive,
        CreatedAt = restaurant.CreatedAt,
        UpdatedAt = restaurant.UpdatedAt,
        IsDeleted = restaurant.IsDeleted,
        DeletedAt = restaurant.DeletedAt,
        AverageRating = averageRating,
        ReviewCount = reviewCount
    };

    /// <summary>
    /// Tager de oplysninger, klienten har sendt for en NY restaurant, og bygger en rigtig
    /// Restaurant-model ud fra dem, som bagefter kan gemmes i databasen.
    /// </summary>
    /// <returns>En ny Restaurant, klar til at blive gemt (har endnu ikke et Id).</returns>
    public static Restaurant ToRestaurant(this CreateRestaurantDto dto) => new()
    {
        UserId = dto.UserId,
        Name = dto.Name,
        Description = dto.Description,
        Address = dto.Address,
        Latitude = dto.Latitude,
        Longitude = dto.Longitude,
        ImageUrl = dto.ImageUrl,
        IsActive = dto.IsActive
    };
}
