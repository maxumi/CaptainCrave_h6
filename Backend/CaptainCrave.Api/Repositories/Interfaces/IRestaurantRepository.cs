using Api.Models;

namespace Api.Repositories;

// Definerer databaseoperationer for restauranter.
public interface IRestaurantRepository
{
    // Henter alle aktive restauranter.
    Task<IEnumerable<Restaurant>> GetAllAsync();

    // Henter en restaurant ud fra ID. Soft-deleted restauranter medtages ikke.
    Task<Restaurant?> GetByIdAsync(int id);

    // Henter en restaurant ud fra ID, også hvis den er soft-deleted.
    Task<Restaurant?> GetByIdIncludingDeletedAsync(int id);

    // Henter alle soft-deleted restauranter, så de kan vises og gendannes.
    Task<IEnumerable<Restaurant>> GetDeletedAsync();

    // Henter alle restauranter der tilhører den angivne bruger.
    Task<IEnumerable<Restaurant>> GetByUserIdAsync(int userId);

    // Henter én restaurant der tilhører den angivne bruger, eller null hvis ingen findes.
    Task<Restaurant?> GetSingleByUserIdAsync(int userId);

    // Gemmer en ny restaurant og returnerer den med det genererede ID.
    Task<Restaurant> CreateAsync(Restaurant restaurant);

    // Opdaterer en eksisterende restaurant og returnerer den opdaterede entity.
    Task<Restaurant> UpdateAsync(Restaurant restaurant);

    // Soft-deleter restauranten samt dens menuer og menu-items uden at fjerne rækkerne permanent.
    Task SoftDeleteAsync(Restaurant restaurant);

    // Gendanner restauranten samt dens soft-deleted menuer og menu-items.
    Task RestoreAsync(Restaurant restaurant);

    // Sletter restauranten permanent.
    // Databasen cascade-sletter de tilhørende menuer og menu-items.
    Task HardDeleteAsync(Restaurant restaurant);
}
