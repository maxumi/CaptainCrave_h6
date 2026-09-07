using Api.DTOs;

namespace Api.Services;

// Definerer forretningslogik for menu-items.
public interface IMenuItemService
{
    // Henter alle menu-items for den angivne restaurant som DTO'er.
    Task<IEnumerable<MenuItemDto>> GetByRestaurantIdAsync(int restaurantId);

    // Henter alle menu-items for den angivne menu som DTO'er.
    Task<IEnumerable<MenuItemDto>> GetByMenuIdAsync(int menuId);

    // Henter soft-deleted menu-items for en restaurant,
    // hvis brugeren ejer restauranten eller er administrator.
    Task<IEnumerable<MenuItemDto>?> GetDeletedByRestaurantIdAsync(int restaurantId, int userId, bool isAdmin);

    // Validerer og opretter et nyt menu-item og returnerer det som DTO.
    Task<MenuItemDto> CreateAsync(CreateMenuItemDto dto);

    // Opdaterer et menu-item, hvis det findes og brugeren har adgang.
    Task<MenuItemDto?> UpdateAsync(int id, CreateMenuItemDto dto, int userId, bool isAdmin);

    // Opdaterer billed-URL'en på et menu-item, hvis brugeren har adgang.
    Task<MenuItemDto?> UpdateImageUrlAsync(int id, string imageUrl, int userId, bool isAdmin);

    // Soft-deleter et menu-item, hvis det findes og brugeren har adgang.
    Task<bool> DeleteAsync(int id, int userId, bool isAdmin);

    // Gendanner et soft-deleted menu-item, hvis det findes og brugeren har adgang.
    Task<bool> RestoreAsync(int id, int userId, bool isAdmin);

    // Sletter et menu-item permanent, hvis det findes og brugeren har adgang.
    Task<bool> HardDeleteAsync(int id, int userId, bool isAdmin);
}
