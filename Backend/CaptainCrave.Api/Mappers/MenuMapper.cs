using Api.DTOs;
using Api.Models;

namespace Api.Mappers;

// Denne klasse hjælper os med at bygge om mellem Menu (menukort i databasen)
// og de DTO'er vi sender frem og tilbage til klienten.
public static class MenuMapper
{
    /// <summary>
    /// Tager en menu fra databasen og pakker den om til en MenuDto,
    /// som er den udgave af menuen, klienten må se.
    /// </summary>
    /// <returns>En MenuDto med de samme oplysninger, klar til at blive sendt afsted.</returns>
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
    /// Tager de oplysninger, klienten har sendt for en NY menu, og bygger en rigtig
    /// Menu-model ud fra dem, som bagefter kan gemmes i databasen.
    /// </summary>
    /// <returns>En ny Menu, klar til at blive gemt (har endnu ikke et Id).</returns>
    public static Menu ToMenu(this CreateMenuDto dto) => new()
    {
        RestaurantId = dto.RestaurantId,
        Name = dto.Name
    };
}
