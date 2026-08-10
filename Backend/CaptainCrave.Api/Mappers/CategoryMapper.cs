using Api.DTOs;
using Api.Models;

namespace Api.Mappers;

// Extension methods for mapping between Category models and DTOs.
public static class CategoryMapper
{
    // Maps a Category entity to a CategoryDto for API responses.
    public static CategoryDto ToDto(this Category category) => new()
    {
        Id = category.Id,
        RestaurantId = category.RestaurantId,
        Name = category.Name
    };

    // Maps a CreateCategoryDto to a Category entity ready to be persisted.
    public static Category ToCategory(this CreateCategoryDto dto) => new()
    {
        RestaurantId = dto.RestaurantId,
        Name = dto.Name
    };
}
