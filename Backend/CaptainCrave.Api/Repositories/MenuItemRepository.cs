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

    // Fetches a single menu item by primary key.
    public async Task<MenuItem?> GetByIdAsync(int id) =>
        await _db.MenuItems.AsNoTracking().FirstOrDefaultAsync(m => m.Id == id);

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
        _db.MenuItems.Update(menuItem);
        await _db.SaveChangesAsync();
        return menuItem;
    }

    // Deletes an existing menu item row.
    public async Task DeleteAsync(MenuItem menuItem)
    {
        _db.MenuItems.Remove(menuItem);
        await _db.SaveChangesAsync();
    }
}
