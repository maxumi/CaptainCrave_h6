using Api.DTOs;
using Api.Models;

namespace Api.Mappers;

// Extension methods for mapping between MenuItem models and DTOs.
public static class MenuItemMapper
{
    // Maps a MenuItem entity to a MenuItemDto for API responses.
    public static MenuItemDto ToDto(this MenuItem menuItem) => new()
    {
        Id = menuItem.Id,
        RestaurantId = menuItem.RestaurantId,
        CategoryId = menuItem.CategoryId,
        Name = menuItem.Name,
        Description = menuItem.Description,
        Price = menuItem.Price,
        ImageUrl = menuItem.ImageUrl,
        IsAvailable = menuItem.IsAvailable
    };

    // Maps a CreateMenuItemDto to a MenuItem entity
    public static MenuItem ToMenuItem(this CreateMenuItemDto dto) => new()
    {
        RestaurantId = dto.RestaurantId,
        CategoryId = dto.CategoryId,
        Name = dto.Name,
        Description = dto.Description,
        Price = dto.Price,
        ImageUrl = dto.ImageUrl,
        IsAvailable = dto.IsAvailable
    };
}
