using Api.Models.Enums;
using System.Security.Claims;

namespace Api.Services;

// Nogle små hjælpe-metoder, der læser oplysninger ud af den indloggede brugers JWT-token (claims).
public static class ClaimsPrincipalExtensions
{
    /// <summary>
    /// Læser bruger-id'et ud af den indloggede brugers claims (gemt i JWT-tokenet, da man loggede ind).
    /// </summary>
    /// <returns>Bruger-id'et som et helt tal.</returns>
    public static int GetId(this ClaimsPrincipal user)
    {
        var id = user.FindFirstValue(ClaimTypes.NameIdentifier);
        return int.Parse(id);
    }

    /// <summary>
    /// Læser brugerens rolle (Customer, Restaurant eller Admin) ud af claims.
    /// Kaster en fejl, hvis rollen mangler eller ikke er en af de roller, vi kender.
    /// </summary>
    /// <returns>Brugerens rolle som en UserRole-værdi.</returns>
    public static UserRole GetRole(this ClaimsPrincipal user)
    {
        var role = user.FindFirstValue(ClaimTypes.Role);

        if (!Enum.TryParse<UserRole>(role, out var userRole))
            throw new UnauthorizedAccessException("Invalid user role.");

        return userRole;
    }
}
