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
    /// Fetches a single menu by primary key.
    /// </summary>
    /// <param name="id">The menu's id.</param>
    public async Task<Menu?> GetByIdAsync(int id) =>
        await _db.Menus.AsNoTracking().FirstOrDefaultAsync(m => m.Id == id);

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
    /// Removes a menu row and allows the database cascade to delete associated menu items.
    /// </summary>
    /// <param name="menu">The entity to delete.</param>
    public async Task DeleteAsync(Menu menu)
    {
        _db.Menus.Remove(menu);
        await _db.SaveChangesAsync();
    }
}
