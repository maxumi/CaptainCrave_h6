using Api.DTOs;
using Api.Models;

namespace Api.Mappers;

// Extension methods for mapping between Restaurant models and DTOs.
public static class RestaurantMapper
{
    // Maps a Restaurant entity to a RestaurantDto for API responses.
    public static RestaurantDto ToDto(this Restaurant restaurant) => new()
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
        DeletedAt = restaurant.DeletedAt
    };

    // Maps a CreateRestaurantDto to a Restaurant entity ready to be persisted.
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
