using Api.DTOs;
using Api.Models;

namespace Api.Mappers;

// Extension methods for mapping between Menu models and DTOs.
public static class MenuMapper
{
    /// <summary>
    /// Maps a Menu entity to a MenuDto for API responses.
    /// </summary>
    /// <param name="menu">The loaded entity to convert.</param>
    public static MenuDto ToDto(this Menu menu) => new()
    {
        Id = menu.Id,
        RestaurantId = menu.RestaurantId,
        Name = menu.Name,
        IsDeleted = menu.IsDeleted,
        DeletedAt = menu.DeletedAt,
        CreatedAt = menu.CreatedAt,
        UpdatedAt = menu.UpdatedAt
    };

    /// <summary>
    /// Maps a CreateMenuDto to a Menu entity ready to be persisted.
    /// </summary>
    /// <param name="dto">The validated request payload with the restaurant id and name to save.</param>
    public static Menu ToMenu(this CreateMenuDto dto) => new()
    {
        RestaurantId = dto.RestaurantId,
        Name = dto.Name
    };
}
