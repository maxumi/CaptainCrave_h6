using Api.Data;
using Api.Models;
using Microsoft.EntityFrameworkCore;

namespace Api.Repositories;

// EF Core implementation of restaurant data access.
public class RestaurantRepository(AppDbContext db) : IRestaurantRepository
{
    private readonly AppDbContext _db = db;

    // Fetches every restaurant from the database.
    public async Task<IEnumerable<Restaurant>> GetAllAsync() =>
        await _db.Restaurants.AsNoTracking().ToListAsync();

    // Fetches a single restaurant by primary key. Excludes soft-deleted restaurants.
    public async Task<Restaurant?> GetByIdAsync(int id) =>
        await _db.Restaurants.AsNoTracking().FirstOrDefaultAsync(r => r.Id == id);

    // Fetches a single restaurant by primary key, including soft-deleted ones.
    public async Task<Restaurant?> GetByIdIncludingDeletedAsync(int id) =>
        await _db.Restaurants.IgnoreQueryFilters().AsNoTracking().FirstOrDefaultAsync(r => r.Id == id);

    // Fetches every soft-deleted restaurant.
    public async Task<IEnumerable<Restaurant>> GetDeletedAsync() =>
        await _db.Restaurants.IgnoreQueryFilters().AsNoTracking().Where(r => r.IsDeleted).ToListAsync();

    // Fetches all restaurants owned by a given user.
    public async Task<IEnumerable<Restaurant>> GetByUserIdAsync(int userId) =>
        await _db.Restaurants.AsNoTracking().Where(r => r.UserId == userId).ToListAsync();

    // Fetches a single restaurant owned by a given user.
    public async Task<Restaurant?> GetSingleByUserIdAsync(int userId) =>
        await _db.Restaurants.AsNoTracking().FirstOrDefaultAsync(r => r.UserId == userId);

    // Inserts a new restaurant row and returns it with its generated ID.
    public async Task<Restaurant> CreateAsync(Restaurant restaurant)
    {
        _db.Restaurants.Add(restaurant);
        await _db.SaveChangesAsync();
        return restaurant;
    }

    // Updates an existing restaurant row and returns the updated entity.
    public async Task<Restaurant> UpdateAsync(Restaurant restaurant)
    {
        restaurant.UpdatedAt = DateTime.UtcNow;
        _db.Restaurants.Update(restaurant);
        await _db.SaveChangesAsync();
        return restaurant;
    }

    // Marks the restaurant, its menus, and its menu items as deleted instead of removing the rows.
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

    // Un-marks the restaurant and its soft-deleted menus and menu items as deleted.
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

    // Permanently removes a restaurant row and allows the database cascade to delete its menus and menu items.
    public async Task HardDeleteAsync(Restaurant restaurant)
    {
        _db.Restaurants.Remove(restaurant);
        await _db.SaveChangesAsync();
    }
}
