using Api.DTOs;
using Api.Mappers;
using Api.Repositories;

namespace Api.Services;

// Handles business logic for menu item operations.
public class MenuItemService(IMenuItemRepository menuItemRepository, IRestaurantRepository restaurantRepository, IMenuService menuService) : IMenuItemService
{
    private readonly IMenuItemRepository _menuItemRepository = menuItemRepository;
    private readonly IRestaurantRepository _restaurantRepository = restaurantRepository;
    private readonly IMenuService _menuService = menuService;

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

    // Deletes an existing menu item when authorized.
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

        await _menuItemRepository.DeleteAsync(existing);
        return true;
    }

    // Resolves the restaurant that owns the menu item's menu, then checks the user owns that restaurant.
    private async Task<bool> UserOwnsMenuItemRestaurantAsync(int userId, int menuId)
    {
        var menu = await _menuService.GetByIdAsync(menuId);
        return menu is not null && await UserOwnsRestaurantAsync(userId, menu.RestaurantId);
    }

    private async Task<bool> UserOwnsRestaurantAsync(int userId, int restaurantId)
    {
        var restaurants = await _restaurantRepository.GetByUserIdAsync(userId);
        return restaurants.Any(r => r.Id == restaurantId);
    }
}
