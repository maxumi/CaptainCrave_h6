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

        if (!isAdmin)
        {
            var restaurants = await _restaurantRepository.GetByUserIdAsync(userId);
            var ownsRestaurant = restaurants.Any(r => r.Id == menu.RestaurantId);
            if (!ownsRestaurant)
                return false;
        }

        await _menuRepository.DeleteAsync(menu);
        return true;
    }
}
