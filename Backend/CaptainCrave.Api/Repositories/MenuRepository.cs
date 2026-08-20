using Api.Data;
using Api.Models;
using Microsoft.EntityFrameworkCore;

namespace Api.Repositories;

// EF Core implementation of menu data access.
public class MenuRepository(AppDbContext db) : IMenuRepository
{
    private readonly AppDbContext _db = db;

    /// <summary>
    /// Fetches all menus for the given restaurant.
    /// </summary>
    /// <param name="restaurantId">The owning restaurant's id.</param>
    public async Task<IEnumerable<Menu>> GetByRestaurantIdAsync(int restaurantId) =>
        await _db.Menus.AsNoTracking().Where(m => m.RestaurantId == restaurantId).ToListAsync();

    /// <summary>
    /// Fetches a single menu by primary key. Excludes soft-deleted menus.
    /// </summary>
    /// <param name="id">The menu's id.</param>
    public async Task<Menu?> GetByIdAsync(int id) =>
        await _db.Menus.AsNoTracking().FirstOrDefaultAsync(m => m.Id == id);

    /// <summary>
    /// Fetches a single menu by primary key, including soft-deleted ones.
    /// </summary>
    /// <param name="id">The menu's id.</param>
    public async Task<Menu?> GetByIdIncludingDeletedAsync(int id) =>
        await _db.Menus.IgnoreQueryFilters().AsNoTracking().FirstOrDefaultAsync(m => m.Id == id);

    /// <summary>
    /// Fetches the soft-deleted menus for a restaurant.
    /// </summary>
    /// <param name="restaurantId">The owning restaurant's id.</param>
    public async Task<IEnumerable<Menu>> GetDeletedByRestaurantIdAsync(int restaurantId) =>
        await _db.Menus.IgnoreQueryFilters().AsNoTracking()
            .Where(m => m.RestaurantId == restaurantId && m.IsDeleted)
            .ToListAsync();

    /// <summary>
    /// Inserts a new menu row and returns it with its generated ID.
    /// </summary>
    /// <param name="menu">The entity to insert.</param>
    public async Task<Menu> CreateAsync(Menu menu)
    {
        _db.Menus.Add(menu);
        await _db.SaveChangesAsync();
        return menu;
    }

    /// <summary>
    /// Marks the menu and its menu items as deleted instead of removing the rows.
    /// </summary>
    /// <param name="menu">The entity to soft delete.</param>
    public async Task SoftDeleteAsync(Menu menu)
    {
        SoftDeleteHelper.MarkDeleted(menu);
        _db.Menus.Update(menu);

        var items = await _db.MenuItems.Where(mi => mi.MenuId == menu.Id).ToListAsync();
        foreach (var item in items)
            SoftDeleteHelper.MarkDeleted(item);

        await _db.SaveChangesAsync();
    }

    /// <summary>
    /// Un-marks the menu and its soft-deleted menu items as deleted.
    /// </summary>
    /// <param name="menu">The entity to restore.</param>
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

    /// <summary>
    /// Permanently removes a menu row and allows the database cascade to delete associated menu items.
    /// </summary>
    /// <param name="menu">The entity to delete.</param>
    public async Task HardDeleteAsync(Menu menu)
    {
        _db.Menus.Remove(menu);
        await _db.SaveChangesAsync();
    }
}
