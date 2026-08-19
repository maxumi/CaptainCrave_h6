using Api.Models;

namespace Api.Repositories;

// Defines database operations used for user profile data
public interface IUserRepository
{
    Task<User?> GetByIdAsync(int id);

    Task<User?> UpdateProfileAsync(
        int userId,
        string name,
        string address,
        double? latitude,
        double? longitude);
}



// // Defines the database operations for users
// public interface IUserRepository
// {
//     Task<User?> GetByIdAsync(int id);
//     Task<User?> GetByEmailAsync(string email);
//     Task<bool> EmailExistsAsync(string email);
//     Task<User> CreateAsync(User user);
//     Task<User?> UpdateProfileAsync(int userId, string name, string address, double? latitude, double? longitude);
// }
