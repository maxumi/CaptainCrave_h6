using Api.DTOs;
using Api.Mappers;
using Api.Repositories;

namespace Api.Services;

// Handles business logic for category operations.
public class CategoryService(ICategoryRepository categoryRepository) : ICategoryService
{
    private readonly ICategoryRepository _categoryRepository = categoryRepository;

    // Retrieves all categories for a restaurant and maps them to DTOs.
    public async Task<IEnumerable<CategoryDto>> GetByRestaurantIdAsync(int restaurantId)
    {
        var categories = await _categoryRepository.GetByRestaurantIdAsync(restaurantId);
        return categories.Select(c => c.ToDto());
    }

    // Retrieves all categories for a menu and maps them to DTOs.
    public async Task<IEnumerable<CategoryDto>> GetByMenuIdAsync(int menuId)
    {
        var categories = await _categoryRepository.GetByMenuIdAsync(menuId);
        return categories.Select(c => c.ToDto());
    }

    // Maps the DTO to a model, saves it, and returns the created category as a DTO.
    public async Task<CategoryDto> CreateAsync(CreateCategoryDto dto)
    {
        var category = dto.ToCategory();
        var created = await _categoryRepository.CreateAsync(category);
        return created.ToDto();
    }
}
