using Api.DTOs;
using Api.Models;

namespace Api.Mappers;

// Extension methods for mapping between Menu models and DTOs.
public static class MenuMapper
{
    // Maps a Menu entity to a MenuDto for API responses.
    public static MenuDto ToDto(this Menu menu) => new()
    {
        Id = menu.Id,
        RestaurantId = menu.RestaurantId,
        Name = menu.Name
    };

    // Maps a CreateMenuDto to a Menu entity ready to be persisted.
    public static Menu ToMenu(this CreateMenuDto dto) => new()
    {
        RestaurantId = dto.RestaurantId,
        Name = dto.Name
    };
}
