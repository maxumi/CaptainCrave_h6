using Api.DTOs;

namespace Api.Services;

// Defines business logic operations for categories.
public interface ICategoryService
{
    // Returns all categories for the given restaurant as DTOs.
    Task<IEnumerable<CategoryDto>> GetByRestaurantIdAsync(int restaurantId);

    // Returns all categories for the given menu as DTOs.
    Task<IEnumerable<CategoryDto>> GetByMenuIdAsync(int menuId);

    // Returns the soft-deleted categories for a restaurant, when the caller owns it or is an admin.
    Task<IEnumerable<CategoryDto>?> GetDeletedByRestaurantIdAsync(int restaurantId, int userId, bool isAdmin);

    // Validates, creates, and returns the new category as a DTO.
    Task<CategoryDto> CreateAsync(CreateCategoryDto dto);

    // Soft deletes a category if found and authorized. The category can be restored later.
    Task<bool> DeleteAsync(int id, int userId, bool isAdmin);

    // Restores a soft-deleted category if found and authorized.
    Task<bool> RestoreAsync(int id, int userId, bool isAdmin);

    // Permanently deletes a category (soft-deleted or not) if found and authorized.
    Task<bool> HardDeleteAsync(int id, int userId, bool isAdmin);
}
