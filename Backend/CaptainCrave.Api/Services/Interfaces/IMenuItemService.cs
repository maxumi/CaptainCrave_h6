using Api.DTOs;

namespace Api.Services;

// Defines business logic operations for menu items.
public interface IMenuItemService
{
    // Returns all menu items for the given restaurant as DTOs.
    Task<IEnumerable<MenuItemDto>> GetByRestaurantIdAsync(int restaurantId);

    // Validates, creates, and returns the new menu item as a DTO.
    Task<MenuItemDto> CreateAsync(CreateMenuItemDto dto);

    // Updates a menu item if found and authorized, then returns it as a DTO.
    Task<MenuItemDto?> UpdateAsync(int id, CreateMenuItemDto dto, int userId, bool isAdmin);

    // Deletes a menu item if found and authorized.
    Task<bool> DeleteAsync(int id, int userId, bool isAdmin);
}
