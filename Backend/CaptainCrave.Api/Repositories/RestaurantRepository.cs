using Api.Data;
using Api.Models;
using Microsoft.EntityFrameworkCore;

namespace Api.Repositories;

// Denne klasse snakker direkte med databasen (via EF Core) og henter/gemmer restauranter.
public class RestaurantRepository(AppDbContext db) : IRestaurantRepository
{
    private readonly AppDbContext _db = db;

    /// <summary>Henter alle restauranter fra databasen.</summary>
    /// <returns>Alle restauranter.</returns>
    public async Task<IEnumerable<Restaurant>> GetAllAsync() =>
        await _db.Restaurants.AsNoTracking().ToListAsync();

    /// <summary>Henter én restaurant ud fra id. Springer automatisk slettede restauranter over.</summary>
    /// <returns>Restauranten, eller null hvis den ikke findes (eller er slettet).</returns>
    public async Task<Restaurant?> GetByIdAsync(int id) =>
        await _db.Restaurants.AsNoTracking().FirstOrDefaultAsync(r => r.Id == id);

    /// <summary>Henter én restaurant ud fra id, også selvom den er blevet slettet (soft delete).</summary>
    /// <returns>Restauranten (slettet eller ej), eller null hvis den slet ikke findes.</returns>
    public async Task<Restaurant?> GetByIdIncludingDeletedAsync(int id) =>
        await _db.Restaurants.IgnoreQueryFilters().AsNoTracking().FirstOrDefaultAsync(r => r.Id == id);

    /// <summary>Henter alle restauranter, der er blevet slettet (lagt i admins "papirkurv").</summary>
    /// <returns>Alle slettede restauranter.</returns>
    public async Task<IEnumerable<Restaurant>> GetDeletedAsync() =>
        await _db.Restaurants.IgnoreQueryFilters().AsNoTracking().Where(r => r.IsDeleted).ToListAsync();

    /// <summary>Henter alle restauranter, som en bestemt bruger ejer.</summary>
    /// <returns>Brugerens restauranter.</returns>
    public async Task<IEnumerable<Restaurant>> GetByUserIdAsync(int userId) =>
        await _db.Restaurants.AsNoTracking().Where(r => r.UserId == userId).ToListAsync();

    /// <summary>Henter den ene restaurant, en bestemt bruger ejer (de fleste brugere ejer højst én).</summary>
    /// <returns>Brugerens restaurant, eller null hvis brugeren ikke ejer nogen.</returns>
    public async Task<Restaurant?> GetSingleByUserIdAsync(int userId) =>
        await _db.Restaurants.AsNoTracking().FirstOrDefaultAsync(r => r.UserId == userId);

    /// <summary>Gemmer en helt ny restaurant-række i databasen og giver den tilbage med et rigtigt id.</summary>
    /// <returns>Den gemte restaurant, nu med et rigtigt Id.</returns>
    public async Task<Restaurant> CreateAsync(Restaurant restaurant)
    {
        _db.Restaurants.Add(restaurant);
        await _db.SaveChangesAsync();
        return restaurant;
    }

    /// <summary>Gemmer ændringer på en restaurant, der allerede findes i databasen.</summary>
    /// <returns>Den opdaterede restaurant.</returns>
    public async Task<Restaurant> UpdateAsync(Restaurant restaurant)
    {
        restaurant.UpdatedAt = DateTime.UtcNow;
        _db.Restaurants.Update(restaurant);
        await _db.SaveChangesAsync();
        return restaurant;
    }

    // Markerer restauranten OG dens menuer OG dens retter som slettet, i stedet for at fjerne
    // rækkerne. Det gør vi manuelt her, så alt, hvad restauranten ejer, forsvinder fra de
    // normale lister på samme tid.
    public async Task SoftDeleteAsync(Restaurant restaurant)
    {
        SoftDeleteHelper.MarkDeleted(restaurant);
        _db.Restaurants.Update(restaurant);

        var menus = await _db.Menus.Where(m => m.RestaurantId == restaurant.Id).ToListAsync();
        foreach (var menu in menus)
            SoftDeleteHelper.MarkDeleted(menu);

        var menuIds = menus.Select(m => m.Id).ToList();
        var items = await _db.MenuItems.Where(mi => menuIds.Contains(mi.MenuId)).ToListAsync();
        foreach (var item in items)
            SoftDeleteHelper.MarkDeleted(item);

        await _db.SaveChangesAsync();
    }

    // Fjerner slette-markeringen på restauranten og på de menuer og retter, der blev slettet
    // sammen med den, så hele restauranten kommer tilbage til live på én gang.
    public async Task RestoreAsync(Restaurant restaurant)
    {
        SoftDeleteHelper.MarkRestored(restaurant);
        _db.Restaurants.Update(restaurant);

        var menus = await _db.Menus.IgnoreQueryFilters()
            .Where(m => m.RestaurantId == restaurant.Id && m.IsDeleted)
            .ToListAsync();
        foreach (var menu in menus)
            SoftDeleteHelper.MarkRestored(menu);

        var menuIds = menus.Select(m => m.Id).ToList();
        var items = await _db.MenuItems.IgnoreQueryFilters()
            .Where(mi => menuIds.Contains(mi.MenuId) && mi.IsDeleted)
            .ToListAsync();
        foreach (var item in items)
            SoftDeleteHelper.MarkRestored(item);

        await _db.SaveChangesAsync();
    }

    // Sletter en restaurant-række FOR ALTID. Databasens egen cascade-regel sørger for også
    // at slette dens menuer og retter.
    public async Task HardDeleteAsync(Restaurant restaurant)
    {
        _db.Restaurants.Remove(restaurant);
        await _db.SaveChangesAsync();
    }
}
