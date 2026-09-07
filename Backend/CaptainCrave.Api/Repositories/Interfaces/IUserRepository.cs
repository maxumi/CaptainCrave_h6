using Api.Models;

namespace Api.Repositories;

// Definerer databaseoperationer for brugerens profiloplysninger.
public interface IUserRepository
{
    // Henter en bruger ud fra ID, eller null hvis brugeren ikke findes.
    Task<User?> GetByIdAsync(int id);

    // Opdaterer brugerens profiloplysninger og returnerer den opdaterede bruger.
    Task<User?> UpdateProfileAsync(
        int userId,
        string name,
        string address,
        double? latitude,
        double? longitude);
}
