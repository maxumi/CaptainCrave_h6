using Api.DTOs;

namespace Api.Services;

// Defines business logic operations for categories.
public interface ICategoryService
{
    // Returns all categories for the given restaurant as DTOs.
    Task<IEnumerable<CategoryDto>> GetByRestaurantIdAsync(int restaurantId);

    // Validates, creates, and returns the new category as a DTO.
    Task<CategoryDto> CreateAsync(CreateCategoryDto dto);
}
