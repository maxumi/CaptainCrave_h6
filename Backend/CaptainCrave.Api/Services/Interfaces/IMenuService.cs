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
    /// Validates, creates, and returns the new menu as a DTO.
    /// </summary>
    /// <param name="dto">The requested menu's restaurant id and name.</param>
    Task<MenuDto> CreateAsync(CreateMenuDto dto);
}
