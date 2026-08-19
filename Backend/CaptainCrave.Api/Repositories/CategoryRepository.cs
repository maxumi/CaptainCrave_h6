using Api.Data;
using Api.Models;
using Microsoft.EntityFrameworkCore;

namespace Api.Repositories;

// EF Core implementation of category data access.
public class CategoryRepository(AppDbContext db) : ICategoryRepository
{
    private readonly AppDbContext _db = db;

    // Fetches all categories for the given restaurant, across all of its menus.
    public async Task<IEnumerable<Category>> GetByRestaurantIdAsync(int restaurantId) =>
        await _db.Categories.AsNoTracking().Where(c => c.Menu.RestaurantId == restaurantId).ToListAsync();

    // Fetches all categories for the given menu.
    public async Task<IEnumerable<Category>> GetByMenuIdAsync(int menuId) =>
        await _db.Categories.AsNoTracking().Where(c => c.MenuId == menuId).ToListAsync();

    // Fetches a single category by primary key. Excludes soft-deleted categories.
    public async Task<Category?> GetByIdAsync(int id) =>
        await _db.Categories.AsNoTracking().FirstOrDefaultAsync(c => c.Id == id);

    // Fetches a single category by primary key, including soft-deleted ones.
    public async Task<Category?> GetByIdIncludingDeletedAsync(int id) =>
        await _db.Categories.IgnoreQueryFilters().AsNoTracking().FirstOrDefaultAsync(c => c.Id == id);

    // Fetches the soft-deleted categories for a restaurant, across all of its menus.
    public async Task<IEnumerable<Category>> GetDeletedByRestaurantIdAsync(int restaurantId) =>
        await _db.Categories.IgnoreQueryFilters().AsNoTracking()
            .Where(c => c.Menu.RestaurantId == restaurantId && c.IsDeleted)
            .ToListAsync();

    // Inserts a new category row and returns it with its generated ID.
    public async Task<Category> CreateAsync(Category category)
    {
        _db.Categories.Add(category);
        await _db.SaveChangesAsync();
        return category;
    }

    // Marks a category as deleted without removing the row.
    public async Task SoftDeleteAsync(Category category)
    {
        SoftDeleteHelper.MarkDeleted(category);
        _db.Categories.Update(category);
        await _db.SaveChangesAsync();
    }

    // Un-marks a soft-deleted category.
    public async Task RestoreAsync(Category category)
    {
        SoftDeleteHelper.MarkRestored(category);
        _db.Categories.Update(category);
        await _db.SaveChangesAsync();
    }

    // Permanently deletes a category row.
    public async Task HardDeleteAsync(Category category)
    {
        _db.Categories.Remove(category);
        await _db.SaveChangesAsync();
    }
}
