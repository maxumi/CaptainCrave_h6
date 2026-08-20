using Api.DTOs;
using Api.Mappers;
using Api.Repositories;

namespace Api.Services;

// Handles business logic for menu item operations.
public class MenuItemService(IMenuItemRepository menuItemRepository, IRestaurantRepository restaurantRepository, IMenuService menuService, IImageStorageService imageStorageService) : IMenuItemService
{
    private readonly IMenuItemRepository _menuItemRepository = menuItemRepository;
    private readonly IRestaurantRepository _restaurantRepository = restaurantRepository;
    private readonly IMenuService _menuService = menuService;
    private readonly IImageStorageService _imageStorageService = imageStorageService;

    // Retrieves all menu items for a restaurant and maps them to DTOs.
    public async Task<IEnumerable<MenuItemDto>> GetByRestaurantIdAsync(int restaurantId)
    {
        var items = await _menuItemRepository.GetByRestaurantIdAsync(restaurantId);
        return items.Select(m => m.ToDto());
    }

    // Retrieves all menu items for a menu and maps them to DTOs.
    public async Task<IEnumerable<MenuItemDto>> GetByMenuIdAsync(int menuId)
    {
        var items = await _menuItemRepository.GetByMenuIdAsync(menuId);
        return items.Select(m => m.ToDto());
    }

    // Retrieves the soft-deleted menu items for a restaurant, if the caller is allowed to see them.
    public async Task<IEnumerable<MenuItemDto>?> GetDeletedByRestaurantIdAsync(int restaurantId, int userId, bool isAdmin)
    {
        if (!isAdmin && !await UserOwnsRestaurantAsync(userId, restaurantId))
            return null;

        var items = await _menuItemRepository.GetDeletedByRestaurantIdAsync(restaurantId);
        return items.Select(m => m.ToDto());
    }

    // Maps the DTO to a model, saves it, and returns the created menu item as a DTO.
    public async Task<MenuItemDto> CreateAsync(CreateMenuItemDto dto)
    {
        var menuItem = dto.ToMenuItem();
        var created = await _menuItemRepository.CreateAsync(menuItem);
        return created.ToDto();
    }

    // Updates an existing menu item when authorized and returns its DTO.
    public async Task<MenuItemDto?> UpdateAsync(int id, CreateMenuItemDto dto, int userId, bool isAdmin)
    {
        var existing = await _menuItemRepository.GetByIdAsync(id);

        if (existing is null)
            return null;

        if (!isAdmin)
        {
            var ownsRestaurant = await UserOwnsMenuItemRestaurantAsync(userId, existing.MenuId);
            if (!ownsRestaurant)
                return null;
        }

        existing.Name = dto.Name;
        existing.Description = dto.Description;
        existing.Price = dto.Price;
        existing.ImageUrl = dto.ImageUrl;
        existing.IsAvailable = dto.IsAvailable;
        existing.CategoryId = dto.CategoryId;

        // Admins can reassign an item to any menu; Restaurant users are locked to their own.
        if (isAdmin)
            existing.MenuId = dto.MenuId;

        var updated = await _menuItemRepository.UpdateAsync(existing);
        return updated.ToDto();
    }

    // Updates a menu item's image URL when authorized and returns its DTO.
    public async Task<MenuItemDto?> UpdateImageUrlAsync(int id, string imageUrl, int userId, bool isAdmin)
    {
        var existing = await _menuItemRepository.GetByIdAsync(id);

        if (existing is null)
            return null;

        if (!isAdmin)
        {
            var ownsRestaurant = await UserOwnsMenuItemRestaurantAsync(userId, existing.MenuId);
            if (!ownsRestaurant)
                return null;
        }

        var previousImageUrl = existing.ImageUrl;
        existing.ImageUrl = imageUrl;
        var updated = await _menuItemRepository.UpdateAsync(existing);
        _imageStorageService.Delete(previousImageUrl);
        return updated.ToDto();
    }

    // Soft deletes an existing menu item when authorized. The item can be restored later.
    public async Task<bool> DeleteAsync(int id, int userId, bool isAdmin)
    {
        var existing = await _menuItemRepository.GetByIdAsync(id);

        if (existing is null)
            return false;

        if (!isAdmin)
        {
            var ownsRestaurant = await UserOwnsMenuItemRestaurantAsync(userId, existing.MenuId);
            if (!ownsRestaurant)
                return false;
        }

        await _menuItemRepository.SoftDeleteAsync(existing);
        return true;
    }

    // Restores a previously soft-deleted menu item when authorized.
    public async Task<bool> RestoreAsync(int id, int userId, bool isAdmin)
    {
        var existing = await _menuItemRepository.GetByIdIncludingDeletedAsync(id);

        if (existing is null || !existing.IsDeleted)
            return false;

        if (!isAdmin)
        {
            var ownsRestaurant = await UserOwnsMenuItemRestaurantAsync(userId, existing.MenuId);
            if (!ownsRestaurant)
                return false;
        }

        await _menuItemRepository.RestoreAsync(existing);
        return true;
    }

    // Permanently deletes a menu item, soft-deleted or not, when authorized.
    public async Task<bool> HardDeleteAsync(int id, int userId, bool isAdmin)
    {
        var existing = await _menuItemRepository.GetByIdIncludingDeletedAsync(id);

        if (existing is null)
            return false;

        if (!isAdmin)
        {
            var ownsRestaurant = await UserOwnsMenuItemRestaurantAsync(userId, existing.MenuId);
            if (!ownsRestaurant)
                return false;
        }

        await _menuItemRepository.HardDeleteAsync(existing);
        return true;
    }

    // Resolves the restaurant that owns the menu item's menu, then checks the user owns that restaurant.
    // Uses the "including deleted" lookup so ownership can still be resolved for a soft-deleted menu.
    private async Task<bool> UserOwnsMenuItemRestaurantAsync(int userId, int menuId)
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
