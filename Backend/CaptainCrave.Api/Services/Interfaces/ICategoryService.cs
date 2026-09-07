using Api.DTOs;

namespace Api.Services;

// Definerer forretningslogik for kategorier.
public interface ICategoryService
{
    // Henter alle kategorier for den angivne restaurant som DTO'er.
    Task<IEnumerable<CategoryDto>> GetByRestaurantIdAsync(int restaurantId);

    // Henter alle kategorier for den angivne menu som DTO'er.
    Task<IEnumerable<CategoryDto>> GetByMenuIdAsync(int menuId);

    // Henter soft-deleted kategorier for en restaurant,
    // hvis brugeren ejer restauranten eller er administrator.
    Task<IEnumerable<CategoryDto>?> GetDeletedByRestaurantIdAsync(int restaurantId, int userId, bool isAdmin);

    // Validerer og opretter en ny kategori og returnerer den som DTO.
    Task<CategoryDto> CreateAsync(CreateCategoryDto dto);

    // Soft-deleter en kategori, hvis den findes og brugeren har adgang.
    Task<bool> DeleteAsync(int id, int userId, bool isAdmin);

    // Gendanner en soft-deleted kategori, hvis den findes og brugeren har adgang.
    Task<bool> RestoreAsync(int id, int userId, bool isAdmin);

    // Sletter en kategori permanent, hvis den findes og brugeren har adgang.
    Task<bool> HardDeleteAsync(int id, int userId, bool isAdmin);
}
