using Api.DTOs;
using Api.Models;

namespace Api.Mappers;

// Denne klasse hjælper os med at bygge om mellem MenuItem (en ret i databasen)
// og de DTO'er vi sender frem og tilbage til klienten.
public static class MenuItemMapper
{
    /// <summary>
    /// Tager en ret (MenuItem) fra databasen og pakker den om til en MenuItemDto,
    /// som er den udgave af retten, klienten må se.
    /// </summary>
    /// <returns>En MenuItemDto med de samme oplysninger, klar til at blive sendt afsted.</returns>
    public static MenuItemDto ToDto(this MenuItem menuItem) => new()
    {
        Id = menuItem.Id,
        MenuId = menuItem.MenuId,
        CategoryId = menuItem.CategoryId,
        Name = menuItem.Name,
        Description = menuItem.Description,
        Price = menuItem.Price,
        ImageUrl = menuItem.ImageUrl,
        IsAvailable = menuItem.IsAvailable,
        IsDeleted = menuItem.IsDeleted,
        DeletedAt = menuItem.DeletedAt,
        CreatedAt = menuItem.CreatedAt,
        UpdatedAt = menuItem.UpdatedAt
    };

    /// <summary>
    /// Tager de oplysninger, klienten har sendt for en NY ret, og bygger en rigtig
    /// MenuItem-model ud fra dem, som bagefter kan gemmes i databasen.
    /// </summary>
    /// <returns>En ny MenuItem, klar til at blive gemt (har endnu ikke et Id).</returns>
    public static MenuItem ToMenuItem(this CreateMenuItemDto dto) => new()
    {
        MenuId = dto.MenuId,
        CategoryId = dto.CategoryId,
        Name = dto.Name,
        Description = dto.Description,
        Price = dto.Price,
        ImageUrl = dto.ImageUrl,
        IsAvailable = dto.IsAvailable
    };
}
