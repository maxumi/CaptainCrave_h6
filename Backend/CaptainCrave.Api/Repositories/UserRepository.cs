using Api.Data;
using Api.Models;
using Microsoft.EntityFrameworkCore;

namespace Api.Repositories;

// Handles database queries for user profile data
public class UserRepository(AppDbContext db) : IUserRepository
{
    // Returns the user with the given id, or null if not found
    public Task<User?> GetByIdAsync(int id) =>
        db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == id);

    // Updates profile fields for a user and returns the updated entity
    public async Task<User?> UpdateProfileAsync(
        int userId,
        string name,
        string address,
        double? latitude,
        double? longitude)
    {
        var user = await db.Users.FirstOrDefaultAsync(u => u.Id == userId);

        if (user is null)
            return null;

        user.Name = name;
        user.Address = address;
        user.Latitude = latitude;
        user.Longitude = longitude;

        await db.SaveChangesAsync();

        return user;
    }
}











// // Handles all database queries for the users table
// public class UserRepository(AppDbContext db) : IUserRepository
// {
//     // Returns the user with the given id, or null if not found
//     public Task<User?> GetByIdAsync(int id) =>
//         db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == id);

//     // Returns the user with the given email, or null if not found
//     public Task<User?> GetByEmailAsync(string email) =>
//         db.Users.FirstOrDefaultAsync(u => u.Email == email);

//     // Returns true if a user with that email already exists
//     public Task<bool> EmailExistsAsync(string email) =>
//         db.Users.AnyAsync(u => u.Email == email);

//     // Saves the new user to the database and returns it with the generated Id
//     public async Task<User> CreateAsync(User user)
//     {
//         db.Users.Add(user);
//         await db.SaveChangesAsync();
//         return user;
//     }

//     // Updates profile fields for a user and returns the updated entity.
//     public async Task<User?> UpdateProfileAsync(int userId, string name, string address, double? latitude, double? longitude)
//     {
//         var user = await db.Users.FirstOrDefaultAsync(u => u.Id == userId);
//         if (user is null)
//             return null;

//         user.Name = name;
//         user.Address = address;
//         user.Latitude = latitude;
//         user.Longitude = longitude;

//         await db.SaveChangesAsync();
//         return user;
//     }
// }
