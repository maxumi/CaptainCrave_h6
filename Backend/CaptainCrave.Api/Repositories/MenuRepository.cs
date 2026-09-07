using Api.Data;
using Api.Models;
using Microsoft.EntityFrameworkCore;

namespace Api.Repositories;

// Denne klasse snakker direkte med databasen (via EF Core) og henter/gemmer menuer.
public class MenuRepository(AppDbContext db) : IMenuRepository
{
    private readonly AppDbContext _db = db;

    /// <summary>
    /// Henter alle menuer, der hører til en restaurant.
    /// </summary>
    /// <returns>Alle menuer for restauranten.</returns>
    public async Task<IEnumerable<Menu>> GetByRestaurantIdAsync(int restaurantId) =>
        await _db.Menus.AsNoTracking().Where(m => m.RestaurantId == restaurantId).ToListAsync();

    /// <summary>
    /// Henter én menu ud fra id. Springer automatisk slettede menuer over.
    /// </summary>
    /// <returns>Menuen, eller null hvis den ikke findes (eller er slettet).</returns>
    public async Task<Menu?> GetByIdAsync(int id) =>
        await _db.Menus.AsNoTracking().FirstOrDefaultAsync(m => m.Id == id);

    /// <summary>
    /// Henter én menu ud fra id, også selvom den er blevet slettet (soft delete).
    /// </summary>
    /// <returns>Menuen (slettet eller ej), eller null hvis den slet ikke findes.</returns>
    public async Task<Menu?> GetByIdIncludingDeletedAsync(int id) =>
        await _db.Menus.IgnoreQueryFilters().AsNoTracking().FirstOrDefaultAsync(m => m.Id == id);

    /// <summary>
    /// Henter de menuer, der er slettet (lagt i "papirkurven") for en restaurant.
    /// </summary>
    /// <returns>De slettede menuer for restauranten.</returns>
    public async Task<IEnumerable<Menu>> GetDeletedByRestaurantIdAsync(int restaurantId) =>
        await _db.Menus.IgnoreQueryFilters().AsNoTracking()
            .Where(m => m.RestaurantId == restaurantId && m.IsDeleted)
            .ToListAsync();

    /// <summary>
    /// Gemmer en helt ny menu-række i databasen og giver den tilbage med et rigtigt id.
    /// </summary>
    /// <returns>Den gemte menu, nu med et rigtigt Id.</returns>
    public async Task<Menu> CreateAsync(Menu menu)
    {
        _db.Menus.Add(menu);
        await _db.SaveChangesAsync();
        return menu;
    }

    // Markerer både menuen OG dens retter som slettet, i stedet for at fjerne rækkerne.
    // Det gør vi manuelt her, så alle retterne på menuen også forsvinder fra de normale
    // lister, når hele menuen bliver slettet.
    public async Task SoftDeleteAsync(Menu menu)
    {
        SoftDeleteHelper.MarkDeleted(menu);
        _db.Menus.Update(menu);

        var items = await _db.MenuItems.Where(mi => mi.MenuId == menu.Id).ToListAsync();
        foreach (var item in items)
            SoftDeleteHelper.MarkDeleted(item);

        await _db.SaveChangesAsync();
    }

    // Fjerner slette-markeringen på menuen OG på de retter, der blev slettet sammen med den,
    // så hele menuen kommer tilbage til live på én gang.
    public async Task RestoreAsync(Menu menu)
    {
        SoftDeleteHelper.MarkRestored(menu);
        _db.Menus.Update(menu);

        var items = await _db.MenuItems.IgnoreQueryFilters()
            .Where(mi => mi.MenuId == menu.Id && mi.IsDeleted)
            .ToListAsync();
        foreach (var item in items)
            SoftDeleteHelper.MarkRestored(item);

        await _db.SaveChangesAsync();
    }

    // Sletter en menu-række FOR ALTID. Databasens egen cascade-regel sørger for også
    // at slette retterne, der hørte til menuen.
    public async Task HardDeleteAsync(Menu menu)
    {
        _db.Menus.Remove(menu);
        await _db.SaveChangesAsync();
    }
}
