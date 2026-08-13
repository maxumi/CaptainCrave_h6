using Api.Data;
using Api.Models;
using Microsoft.EntityFrameworkCore;

namespace Api.Repositories;

// EF Core implementation of category data access.
public class CategoryRepository(AppDbContext db) : ICategoryRepository
{
    private readonly AppDbContext _db = db;

    // Fetches all categories for the given restaurant.
    public async Task<IEnumerable<Category>> GetByRestaurantIdAsync(int restaurantId) =>
        await _db.Categories.AsNoTracking().Where(c => c.RestaurantId == restaurantId).ToListAsync();

    // Fetches a single category by primary key.
    public async Task<Category?> GetByIdAsync(int id) =>
        await _db.Categories.AsNoTracking().FirstOrDefaultAsync(c => c.Id == id);

    // Inserts a new category row and returns it with its generated ID.
    public async Task<Category> CreateAsync(Category category)
    {
        _db.Categories.Add(category);
        await _db.SaveChangesAsync();
        return category;
    }
}
