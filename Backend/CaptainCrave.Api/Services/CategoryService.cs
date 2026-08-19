using Api.DTOs;
using Api.Mappers;
using Api.Repositories;

namespace Api.Services;

// Handles business logic for category operations.
public class CategoryService(ICategoryRepository categoryRepository, IMenuService menuService, IRestaurantRepository restaurantRepository) : ICategoryService
{
    private readonly ICategoryRepository _categoryRepository = categoryRepository;
    private readonly IMenuService _menuService = menuService;
    private readonly IRestaurantRepository _restaurantRepository = restaurantRepository;

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

    // Retrieves the soft-deleted categories for a restaurant, if the caller is allowed to see them.
    public async Task<IEnumerable<CategoryDto>?> GetDeletedByRestaurantIdAsync(int restaurantId, int userId, bool isAdmin)
    {
        if (!isAdmin && !await UserOwnsRestaurantAsync(userId, restaurantId))
            return null;

        var categories = await _categoryRepository.GetDeletedByRestaurantIdAsync(restaurantId);
        return categories.Select(c => c.ToDto());
    }

    // Maps the DTO to a model, saves it, and returns the created category as a DTO.
    public async Task<CategoryDto> CreateAsync(CreateCategoryDto dto)
    {
        var category = dto.ToCategory();
        var created = await _categoryRepository.CreateAsync(category);
        return created.ToDto();
    }

    // Soft deletes an existing category when authorized. The category can be restored later.
    public async Task<bool> DeleteAsync(int id, int userId, bool isAdmin)
    {
        var existing = await _categoryRepository.GetByIdAsync(id);

        if (existing is null)
            return false;

        if (!isAdmin && !await UserOwnsCategoryRestaurantAsync(userId, existing.MenuId))
            return false;

        await _categoryRepository.SoftDeleteAsync(existing);
        return true;
    }

    // Restores a previously soft-deleted category when authorized.
    public async Task<bool> RestoreAsync(int id, int userId, bool isAdmin)
    {
        var existing = await _categoryRepository.GetByIdIncludingDeletedAsync(id);

        if (existing is null || !existing.IsDeleted)
            return false;

        if (!isAdmin && !await UserOwnsCategoryRestaurantAsync(userId, existing.MenuId))
            return false;

        await _categoryRepository.RestoreAsync(existing);
        return true;
    }

    // Permanently deletes a category, soft-deleted or not, when authorized.
    public async Task<bool> HardDeleteAsync(int id, int userId, bool isAdmin)
    {
        var existing = await _categoryRepository.GetByIdIncludingDeletedAsync(id);

        if (existing is null)
            return false;

        if (!isAdmin && !await UserOwnsCategoryRestaurantAsync(userId, existing.MenuId))
            return false;

        await _categoryRepository.HardDeleteAsync(existing);
        return true;
    }

    // Resolves the restaurant that owns the category's menu, then checks the user owns that restaurant.
    // Uses the "including deleted" lookup so ownership can still be resolved for a soft-deleted menu.
    private async Task<bool> UserOwnsCategoryRestaurantAsync(int userId, int menuId)
    {
        var menu = await _menuService.GetByIdIncludingDeletedAsync(menuId);
        return menu is not null && await UserOwnsRestaurantAsync(userId, menu.RestaurantId);
    }

    private async Task<bool> UserOwnsRestaurantAsync(int userId, int restaurantId)
    {
        var restaurants = await _restaurantRepository.GetByUserIdAsync(userId);
        return restaurants.Any(r => r.Id == restaurantId);
    }
}
