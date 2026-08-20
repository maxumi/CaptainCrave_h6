using Api.Data;
using Api.Models;
using Microsoft.EntityFrameworkCore;

namespace Api.Repositories;

// EF Core implementation of menu item data access.
public class MenuItemRepository(AppDbContext db) : IMenuItemRepository
{
    private readonly AppDbContext _db = db;

    // Fetches all menu items belonging to the given restaurant, across all of its menus.
    public async Task<IEnumerable<MenuItem>> GetByRestaurantIdAsync(int restaurantId) =>
        await _db.MenuItems.AsNoTracking().Where(m => m.Menu.RestaurantId == restaurantId).ToListAsync();

    // Fetches all menu items belonging to the given menu.
    public async Task<IEnumerable<MenuItem>> GetByMenuIdAsync(int menuId) =>
        await _db.MenuItems.AsNoTracking().Where(m => m.MenuId == menuId).ToListAsync();

    // Fetches all menu items under the given category.
    public async Task<IEnumerable<MenuItem>> GetByCategoryIdAsync(int categoryId) =>
        await _db.MenuItems.AsNoTracking().Where(m => m.CategoryId == categoryId).ToListAsync();

    // Fetches a single menu item by primary key. Excludes soft-deleted items.
    public async Task<MenuItem?> GetByIdAsync(int id) =>
        await _db.MenuItems.AsNoTracking().FirstOrDefaultAsync(m => m.Id == id);

    // Fetches a single menu item by primary key, including soft-deleted ones.
    public async Task<MenuItem?> GetByIdIncludingDeletedAsync(int id) =>
        await _db.MenuItems.IgnoreQueryFilters().AsNoTracking().FirstOrDefaultAsync(m => m.Id == id);

    // Fetches the soft-deleted menu items for a restaurant, across all of its menus.
    public async Task<IEnumerable<MenuItem>> GetDeletedByRestaurantIdAsync(int restaurantId) =>
        await _db.MenuItems.IgnoreQueryFilters().AsNoTracking()
            .Where(m => m.Menu.RestaurantId == restaurantId && m.IsDeleted)
            .ToListAsync();

    // Inserts a new menu item row and returns it with its generated ID.
    public async Task<MenuItem> CreateAsync(MenuItem menuItem)
    {
        _db.MenuItems.Add(menuItem);
        await _db.SaveChangesAsync();
        return menuItem;
    }

    // Updates an existing menu item row and returns the updated entity.
    public async Task<MenuItem> UpdateAsync(MenuItem menuItem)
    {
        menuItem.UpdatedAt = DateTime.UtcNow;
        _db.MenuItems.Update(menuItem);
        await _db.SaveChangesAsync();
        return menuItem;
    }

    // Marks a menu item as deleted without removing the row.
    public async Task SoftDeleteAsync(MenuItem menuItem)
    {
        SoftDeleteHelper.MarkDeleted(menuItem);
        _db.MenuItems.Update(menuItem);
        await _db.SaveChangesAsync();
    }

    // Un-marks a soft-deleted menu item.
    public async Task RestoreAsync(MenuItem menuItem)
    {
        SoftDeleteHelper.MarkRestored(menuItem);
        _db.MenuItems.Update(menuItem);
        await _db.SaveChangesAsync();
    }

    // Permanently deletes a menu item row.
    public async Task HardDeleteAsync(MenuItem menuItem)
    {
        _db.MenuItems.Remove(menuItem);
        await _db.SaveChangesAsync();
    }
}
