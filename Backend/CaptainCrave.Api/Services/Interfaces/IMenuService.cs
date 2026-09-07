using Api.DTOs;

namespace Api.Services;

// Definerer forretningslogik for menuer.
public interface IMenuService
{
    /// <summary>
    /// Henter alle menuer for den angivne restaurant som DTO'er.
    /// </summary>
    /// <param name="restaurantId">ID på restauranten der ejer menuerne.</param>
    Task<IEnumerable<MenuDto>> GetByRestaurantIdAsync(int restaurantId);

    /// <summary>
    /// Henter en menu ud fra ID, eller null hvis den ikke findes.
    /// </summary>
    /// <param name="id">Menuens ID.</param>
    Task<MenuDto?> GetByIdAsync(int id);

    /// <summary>
    /// Henter en menu ud fra ID, også hvis den er soft-deleted.
    /// Bruges internt til at finde restaurantens ID, selv hvis menuen er slettet.
    /// </summary>
    /// <param name="id">Menuens ID.</param>
    Task<MenuDto?> GetByIdIncludingDeletedAsync(int id);

    /// <summary>
    /// Henter soft-deleted menuer for en restaurant,
    /// hvis brugeren ejer restauranten eller er administrator.
    /// </summary>
    /// <param name="restaurantId">ID på restauranten der ejer menuerne.</param>
    /// <param name="userId">Den aktuelle brugers ID.</param>
    /// <param name="isAdmin">Angiver om brugeren er administrator.</param>
    Task<IEnumerable<MenuDto>?> GetDeletedByRestaurantIdAsync(int restaurantId, int userId, bool isAdmin);

    /// <summary>
    /// Validerer og opretter en ny menu og returnerer den som DTO.
    /// </summary>
    /// <param name="dto">Data for den menu der skal oprettes.</param>
    Task<MenuDto> CreateAsync(CreateMenuDto dto);

    /// <summary>
    /// Soft-deleter en menu, hvis brugeren ejer restauranten eller er administrator.
    /// </summary>
    /// <param name="id">ID på menuen der skal slettes.</param>
    /// <param name="userId">Den aktuelle brugers ID.</param>
    /// <param name="isAdmin">Angiver om brugeren er administrator.</param>
    Task<bool> DeleteAsync(int id, int userId, bool isAdmin);

    /// <summary>
    /// Gendanner en soft-deleted menu, hvis brugeren ejer restauranten eller er administrator.
    /// </summary>
    /// <param name="id">ID på menuen der skal gendannes.</param>
    /// <param name="userId">Den aktuelle brugers ID.</param>
    /// <param name="isAdmin">Angiver om brugeren er administrator.</param>
    Task<bool> RestoreAsync(int id, int userId, bool isAdmin);

    /// <summary>
    /// Sletter en menu permanent, hvis brugeren ejer restauranten eller er administrator.
    /// </summary>
    /// <param name="id">ID på menuen der skal slettes permanent.</param>
    /// <param name="userId">Den aktuelle brugers ID.</param>
    /// <param name="isAdmin">Angiver om brugeren er administrator.</param>
    Task<bool> HardDeleteAsync(int id, int userId, bool isAdmin);
}
