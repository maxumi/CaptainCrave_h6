using Api.DTOs;

namespace Api.Services;

// Defines business logic operations for menu items.
public interface IMenuItemService
{
    // Returns all menu items for the given restaurant as DTOs.
    Task<IEnumerable<MenuItemDto>> GetByRestaurantIdAsync(int restaurantId);

    // Returns all menu items for the given menu as DTOs.
    Task<IEnumerable<MenuItemDto>> GetByMenuIdAsync(int menuId);

    // Returns the soft-deleted menu items for a restaurant, when the caller owns it or is an admin.
    Task<IEnumerable<MenuItemDto>?> GetDeletedByRestaurantIdAsync(int restaurantId, int userId, bool isAdmin);

    // Validates, creates, and returns the new menu item as a DTO.
    Task<MenuItemDto> CreateAsync(CreateMenuItemDto dto);

    // Updates a menu item if found and authorized, then returns it as a DTO.
    Task<MenuItemDto?> UpdateAsync(int id, CreateMenuItemDto dto, int userId, bool isAdmin);

    // Updates a menu item's image URL if found and authorized, then returns it as a DTO.
    Task<MenuItemDto?> UpdateImageUrlAsync(int id, string imageUrl, int userId, bool isAdmin);

    // Soft deletes a menu item if found and authorized. The item can be restored later.
    Task<bool> DeleteAsync(int id, int userId, bool isAdmin);

    // Restores a soft-deleted menu item if found and authorized.
    Task<bool> RestoreAsync(int id, int userId, bool isAdmin);

    // Permanently deletes a menu item (soft-deleted or not) if found and authorized.
    Task<bool> HardDeleteAsync(int id, int userId, bool isAdmin);
}
