using Api.DTOs;

namespace Api.Services;

// Defines business logic operations for menus.
public interface IMenuService
{
    // Returns all menus for the given restaurant as DTOs.
    Task<IEnumerable<MenuDto>> GetByRestaurantIdAsync(int restaurantId);

    // Returns a single menu DTO by ID, or null if not found.
    Task<MenuDto?> GetByIdAsync(int id);

    // Validates, creates, and returns the new menu as a DTO.
    Task<MenuDto> CreateAsync(CreateMenuDto dto);
}
