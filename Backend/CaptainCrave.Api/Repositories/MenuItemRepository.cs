using Api.Data;
using Api.Models;
using Microsoft.EntityFrameworkCore;

namespace Api.Repositories;

// Denne klasse snakker direkte med databasen (via EF Core) og henter/gemmer retter (menu-punkter).
public class MenuItemRepository(AppDbContext db) : IMenuItemRepository
{
    private readonly AppDbContext _db = db;

    /// <summary>Henter alle retter, der hører til en restaurant, på tværs af alle dens menuer.</summary>
    /// <returns>Alle retter for restauranten.</returns>
    public async Task<IEnumerable<MenuItem>> GetByRestaurantIdAsync(int restaurantId) =>
        await _db.MenuItems.AsNoTracking().Where(m => m.Menu.RestaurantId == restaurantId).ToListAsync();

    /// <summary>Henter alle retter, der hører til én bestemt menu.</summary>
    /// <returns>Alle retter for menuen.</returns>
    public async Task<IEnumerable<MenuItem>> GetByMenuIdAsync(int menuId) =>
        await _db.MenuItems.AsNoTracking().Where(m => m.MenuId == menuId).ToListAsync();

    /// <summary>Henter alle retter, der ligger under én bestemt kategori (fx "Burgere").</summary>
    /// <returns>Alle retter i kategorien.</returns>
    public async Task<IEnumerable<MenuItem>> GetByCategoryIdAsync(int categoryId) =>
        await _db.MenuItems.AsNoTracking().Where(m => m.CategoryId == categoryId).ToListAsync();

    /// <summary>Henter én ret ud fra id. Springer automatisk slettede retter over.</summary>
    /// <returns>Retten, eller null hvis den ikke findes (eller er slettet).</returns>
    public async Task<MenuItem?> GetByIdAsync(int id) =>
        await _db.MenuItems.AsNoTracking().FirstOrDefaultAsync(m => m.Id == id);

    /// <summary>Henter én ret ud fra id, også selvom den er blevet slettet (soft delete).</summary>
    /// <returns>Retten (slettet eller ej), eller null hvis den slet ikke findes.</returns>
    public async Task<MenuItem?> GetByIdIncludingDeletedAsync(int id) =>
        await _db.MenuItems.IgnoreQueryFilters().AsNoTracking().FirstOrDefaultAsync(m => m.Id == id);

    /// <summary>Henter de retter, der er slettet (lagt i "papirkurven") for en restaurant, på tværs af alle dens menuer.</summary>
    /// <returns>De slettede retter for restauranten.</returns>
    public async Task<IEnumerable<MenuItem>> GetDeletedByRestaurantIdAsync(int restaurantId) =>
        await _db.MenuItems.IgnoreQueryFilters().AsNoTracking()
            .Where(m => m.Menu.RestaurantId == restaurantId && m.IsDeleted)
            .ToListAsync();

    /// <summary>Gemmer en helt ny ret-række i databasen og giver den tilbage med et rigtigt id.</summary>
    /// <returns>Den gemte ret, nu med et rigtigt Id.</returns>
    public async Task<MenuItem> CreateAsync(MenuItem menuItem)
    {
        _db.MenuItems.Add(menuItem);
        await _db.SaveChangesAsync();
        return menuItem;
    }

    /// <summary>Gemmer ændringer på en ret, der allerede findes i databasen.</summary>
    /// <returns>Den opdaterede ret.</returns>
    public async Task<MenuItem> UpdateAsync(MenuItem menuItem)
    {
        menuItem.UpdatedAt = DateTime.UtcNow;
        _db.MenuItems.Update(menuItem);
        await _db.SaveChangesAsync();
        return menuItem;
    }

    // Markerer en ret som slettet, uden faktisk at fjerne rækken fra databasen.
    public async Task SoftDeleteAsync(MenuItem menuItem)
    {
        SoftDeleteHelper.MarkDeleted(menuItem);
        _db.MenuItems.Update(menuItem);
        await _db.SaveChangesAsync();
    }

    // Fjerner slette-markeringen på en tidligere slettet ret, så den bliver synlig igen.
    public async Task RestoreAsync(MenuItem menuItem)
    {
        SoftDeleteHelper.MarkRestored(menuItem);
        _db.MenuItems.Update(menuItem);
        await _db.SaveChangesAsync();
    }

    // Sletter en ret-række FOR ALTID fra databasen.
    public async Task HardDeleteAsync(MenuItem menuItem)
    {
        _db.MenuItems.Remove(menuItem);
        await _db.SaveChangesAsync();
    }
}
