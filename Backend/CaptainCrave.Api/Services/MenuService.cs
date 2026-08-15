using Api.DTOs;
using Api.Mappers;
using Api.Repositories;

namespace Api.Services;

// Handles business logic for menu operations.
public class MenuService(IMenuRepository menuRepository) : IMenuService
{
    private readonly IMenuRepository _menuRepository = menuRepository;

    // Retrieves all menus for a restaurant and maps them to DTOs.
    public async Task<IEnumerable<MenuDto>> GetByRestaurantIdAsync(int restaurantId)
    {
        var menus = await _menuRepository.GetByRestaurantIdAsync(restaurantId);
        return menus.Select(m => m.ToDto());
    }

    // Retrieves a single menu by ID and maps it to a DTO.
    public async Task<MenuDto?> GetByIdAsync(int id)
    {
        var menu = await _menuRepository.GetByIdAsync(id);
        return menu?.ToDto();
    }

    // Maps the DTO to a model, saves it, and returns the created menu as a DTO.
    public async Task<MenuDto> CreateAsync(CreateMenuDto dto)
    {
        var menu = dto.ToMenu();
        var created = await _menuRepository.CreateAsync(menu);
        return created.ToDto();
    }
}
