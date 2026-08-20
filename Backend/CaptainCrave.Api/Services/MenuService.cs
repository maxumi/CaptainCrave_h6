using Api.DTOs;
using Api.Mappers;
using Api.Repositories;

namespace Api.Services;

// Handles business logic for menu operations.
public class MenuService(IMenuRepository menuRepository, IRestaurantRepository restaurantRepository) : IMenuService
{
    private readonly IMenuRepository _menuRepository = menuRepository;
    private readonly IRestaurantRepository _restaurantRepository = restaurantRepository;

    /// <summary>
    /// Retrieves all menus for a restaurant and maps them to DTOs.
    /// </summary>
    /// <param name="restaurantId">The owning restaurant's id.</param>
    public async Task<IEnumerable<MenuDto>> GetByRestaurantIdAsync(int restaurantId)
    {
        var menus = await _menuRepository.GetByRestaurantIdAsync(restaurantId);
        return menus.Select(m => m.ToDto());
    }

    /// <summary>
    /// Retrieves a single menu by ID and maps it to a DTO.
    /// </summary>
    /// <param name="id">The menu's id.</param>
    public async Task<MenuDto?> GetByIdAsync(int id)
    {
        var menu = await _menuRepository.GetByIdAsync(id);
        return menu?.ToDto();
    }

    /// <summary>
    /// Retrieves a single menu by ID, including soft-deleted ones, and maps it to a DTO.
    /// </summary>
    /// <param name="id">The menu's id.</param>
    public async Task<MenuDto?> GetByIdIncludingDeletedAsync(int id)
    {
        var menu = await _menuRepository.GetByIdIncludingDeletedAsync(id);
        return menu?.ToDto();
    }

    /// <summary>
    /// Retrieves the soft-deleted menus for a restaurant, if the caller is allowed to see them.
    /// </summary>
    /// <param name="restaurantId">The owning restaurant's id.</param>
    /// <param name="userId">The current user.</param>
    /// <param name="isAdmin">Whether the current user is an admin.</param>
    public async Task<IEnumerable<MenuDto>?> GetDeletedByRestaurantIdAsync(int restaurantId, int userId, bool isAdmin)
    {
        if (!isAdmin && !await UserOwnsRestaurantAsync(userId, restaurantId))
            return null;

        var menus = await _menuRepository.GetDeletedByRestaurantIdAsync(restaurantId);
        return menus.Select(m => m.ToDto());
    }

    /// <summary>
    /// Maps the DTO to a model, saves it, and returns the created menu as a DTO.
    /// </summary>
    /// <param name="dto">The validated request payload with the restaurant id and name to save.</param>
    public async Task<MenuDto> CreateAsync(CreateMenuDto dto)
    {
        var menu = dto.ToMenu();
        var created = await _menuRepository.CreateAsync(menu);
        return created.ToDto();
    }

    public async Task<bool> DeleteAsync(int id, int userId, bool isAdmin)
    {
        var menu = await _menuRepository.GetByIdAsync(id);
        if (menu is null)
            return false;

        if (!isAdmin && !await UserOwnsRestaurantAsync(userId, menu.RestaurantId))
            return false;

        await _menuRepository.SoftDeleteAsync(menu);
        return true;
    }

    /// <summary>
    /// Restores a previously soft-deleted menu when the caller owns that restaurant or is an admin.
    /// </summary>
    /// <param name="id">Menu ID to restore.</param>
    /// <param name="userId">The current user.</param>
    /// <param name="isAdmin">Whether the current user is an admin.</param>
    public async Task<bool> RestoreAsync(int id, int userId, bool isAdmin)
    {
        var menu = await _menuRepository.GetByIdIncludingDeletedAsync(id);
        if (menu is null || !menu.IsDeleted)
            return false;

        if (!isAdmin && !await UserOwnsRestaurantAsync(userId, menu.RestaurantId))
            return false;

        await _menuRepository.RestoreAsync(menu);
        return true;
    }

    /// <summary>
    /// Permanently deletes a menu, soft-deleted or not, when the caller owns that restaurant or is an admin.
    /// </summary>
    /// <param name="id">Menu ID to permanently delete.</param>
    /// <param name="userId">The current user.</param>
    /// <param name="isAdmin">Whether the current user is an admin.</param>
    public async Task<bool> HardDeleteAsync(int id, int userId, bool isAdmin)
    {
        var menu = await _menuRepository.GetByIdIncludingDeletedAsync(id);
        if (menu is null)
            return false;

        if (!isAdmin && !await UserOwnsRestaurantAsync(userId, menu.RestaurantId))
            return false;

        await _menuRepository.HardDeleteAsync(menu);
        return true;
    }

    private async Task<bool> UserOwnsRestaurantAsync(int userId, int restaurantId)
    {
        var restaurants = await _restaurantRepository.GetByUserIdAsync(userId);
        return restaurants.Any(r => r.Id == restaurantId);
    }
}
