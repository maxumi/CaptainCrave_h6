using Api.Data;
using Api.Models;
using Microsoft.EntityFrameworkCore;

namespace Api.Repositories;

// EF Core implementation of menu data access.
public class MenuRepository(AppDbContext db) : IMenuRepository
{
    private readonly AppDbContext _db = db;

    // Fetches all menus for the given restaurant.
    public async Task<IEnumerable<Menu>> GetByRestaurantIdAsync(int restaurantId) =>
        await _db.Menus.AsNoTracking().Where(m => m.RestaurantId == restaurantId).ToListAsync();

    // Fetches a single menu by primary key.
    public async Task<Menu?> GetByIdAsync(int id) =>
        await _db.Menus.AsNoTracking().FirstOrDefaultAsync(m => m.Id == id);

    // Inserts a new menu row and returns it with its generated ID.
    public async Task<Menu> CreateAsync(Menu menu)
    {
        _db.Menus.Add(menu);
        await _db.SaveChangesAsync();
        return menu;
    }
}
