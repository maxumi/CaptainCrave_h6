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

    // Fetches a single restaurant by primary key.
    public async Task<Restaurant?> GetByIdAsync(int id) =>
        await _db.Restaurants.AsNoTracking().FirstOrDefaultAsync(r => r.Id == id);

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
}
