using Api.DTOs;

namespace Api.Services;

// Defines business logic operations for menus.
public interface IMenuService
{
    /// <summary>
    /// Returns all menus for the given restaurant as DTOs.
    /// </summary>
    /// <param name="restaurantId">The owning restaurant's id.</param>
    Task<IEnumerable<MenuDto>> GetByRestaurantIdAsync(int restaurantId);

    /// <summary>
    /// Returns a single menu DTO by ID, or null if not found.
    /// </summary>
    /// <param name="id">The menu's id.</param>
    Task<MenuDto?> GetByIdAsync(int id);

    /// <summary>
    /// Returns a single menu DTO by ID including soft-deleted ones, or null if it never existed.
    /// Used internally to resolve a menu item's restaurant even when its menu has been soft-deleted.
    /// </summary>
    /// <param name="id">The menu's id.</param>
    Task<MenuDto?> GetByIdIncludingDeletedAsync(int id);

    /// <summary>
    /// Returns the soft-deleted menus for a restaurant, when the caller owns that restaurant or is an admin.
    /// </summary>
    /// <param name="restaurantId">The owning restaurant's id.</param>
    /// <param name="userId">The current user.</param>
    /// <param name="isAdmin">Whether the current user is an admin.</param>
    Task<IEnumerable<MenuDto>?> GetDeletedByRestaurantIdAsync(int restaurantId, int userId, bool isAdmin);

    /// <summary>
    /// Validates, creates, and returns the new menu as a DTO.
    /// </summary>
    /// <param name="dto">The requested menu's restaurant id and name.</param>
    Task<MenuDto> CreateAsync(CreateMenuDto dto);

    /// <summary>
    /// Soft deletes a menu when the caller owns that restaurant or is an admin. The menu can be restored later.
    /// </summary>
    /// <param name="id">Menu ID to delete.</param>
    /// <param name="userId">The current user.</param>
    /// <param name="isAdmin">Whether the current user is an admin.</param>
    Task<bool> DeleteAsync(int id, int userId, bool isAdmin);

    /// <summary>
    /// Restores a soft-deleted menu when the caller owns that restaurant or is an admin.
    /// </summary>
    /// <param name="id">Menu ID to restore.</param>
    /// <param name="userId">The current user.</param>
    /// <param name="isAdmin">Whether the current user is an admin.</param>
    Task<bool> RestoreAsync(int id, int userId, bool isAdmin);

    /// <summary>
    /// Permanently deletes a menu (soft-deleted or not) when the caller owns that restaurant or is an admin.
    /// </summary>
    /// <param name="id">Menu ID to permanently delete.</param>
    /// <param name="userId">The current user.</param>
    /// <param name="isAdmin">Whether the current user is an admin.</param>
    Task<bool> HardDeleteAsync(int id, int userId, bool isAdmin);
}
