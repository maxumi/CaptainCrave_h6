using Api.Models;

namespace Api.Services;

// Definerer funktionen til generering af JWT-tokens.
public interface ITokenService
{
    // Genererer et JWT-token for den angivne bruger.
    string GenerateToken(User user);
}
