using Api.Data;
using Api.Models;
using Microsoft.EntityFrameworkCore;

namespace Api.Repositories;

// Denne klasse snakker direkte med databasen (via EF Core) og henter/gemmer kategorier.
public class CategoryRepository(AppDbContext db) : ICategoryRepository
{
    private readonly AppDbContext _db = db;

    /// <summary>
    /// Henter alle kategorier, der hører til en restaurant, på tværs af alle dens menuer
    /// (en restaurant kan have flere menuer, hver med sine egne kategorier).
    /// </summary>
    /// <returns>Alle kategorier for restauranten.</returns>
    public async Task<IEnumerable<Category>> GetByRestaurantIdAsync(int restaurantId) =>
        await _db.Categories.AsNoTracking().Where(c => c.Menu.RestaurantId == restaurantId).ToListAsync();

    /// <summary>
    /// Henter alle kategorier, der hører til én bestemt menu.
    /// </summary>
    /// <returns>Alle kategorier for menuen.</returns>
    public async Task<IEnumerable<Category>> GetByMenuIdAsync(int menuId) =>
        await _db.Categories.AsNoTracking().Where(c => c.MenuId == menuId).ToListAsync();

    /// <summary>
    /// Henter én kategori ud fra id. Springer automatisk slettede kategorier over,
    /// så man ikke ved et uheld får fat i noget, der skulle være i papirkurven.
    /// </summary>
    /// <returns>Kategorien, eller null hvis den ikke findes (eller er slettet).</returns>
    public async Task<Category?> GetByIdAsync(int id) =>
        await _db.Categories.AsNoTracking().FirstOrDefaultAsync(c => c.Id == id);

    /// <summary>
    /// Henter én kategori ud fra id, også selvom den er blevet slettet (soft delete).
    /// Bruges når vi selv skal have fat i en slettet række, fx for at gendanne den.
    /// </summary>
    /// <returns>Kategorien (slettet eller ej), eller null hvis den slet ikke findes.</returns>
    public async Task<Category?> GetByIdIncludingDeletedAsync(int id) =>
        await _db.Categories.IgnoreQueryFilters().AsNoTracking().FirstOrDefaultAsync(c => c.Id == id);

    /// <summary>
    /// Henter de kategorier, der er slettet (lagt i "papirkurven") for en restaurant,
    /// på tværs af alle dens menuer.
    /// </summary>
    /// <returns>De slettede kategorier for restauranten.</returns>
    public async Task<IEnumerable<Category>> GetDeletedByRestaurantIdAsync(int restaurantId) =>
        await _db.Categories.IgnoreQueryFilters().AsNoTracking()
            .Where(c => c.Menu.RestaurantId == restaurantId && c.IsDeleted)
            .ToListAsync();

    /// <summary>
    /// Gemmer en helt ny kategori-række i databasen og giver den tilbage med det id,
    /// databasen selv har fundet på til den.
    /// </summary>
    /// <returns>Den gemte kategori, nu med et rigtigt Id.</returns>
    public async Task<Category> CreateAsync(Category category)
    {
        _db.Categories.Add(category);
        await _db.SaveChangesAsync();
        return category;
    }

    // Markerer en kategori som slettet, uden faktisk at fjerne rækken fra databasen,
    // den bliver bare skjult og kan gendannes senere.
    public async Task SoftDeleteAsync(Category category)
    {
        SoftDeleteHelper.MarkDeleted(category);
        _db.Categories.Update(category);
        await _db.SaveChangesAsync();
    }

    // Fjerner slette-markeringen på en tidligere slettet kategori, så den bliver synlig igen.
    public async Task RestoreAsync(Category category)
    {
        SoftDeleteHelper.MarkRestored(category);
        _db.Categories.Update(category);
        await _db.SaveChangesAsync();
    }

    // Sletter en kategori-række FOR ALTID fra databasen. Der er ingen fortryd-knap efter dette.
    public async Task HardDeleteAsync(Category category)
    {
        _db.Categories.Remove(category);
        await _db.SaveChangesAsync();
    }
}
