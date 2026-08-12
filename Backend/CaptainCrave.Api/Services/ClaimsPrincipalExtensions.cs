using Api.Models.Enums;
using System.Security.Claims;

namespace Api.Services;

public static class ClaimsPrincipalExtensions
{
    /// <summary>
    /// Returns the user ID from the claims associated with the authenticated user.
    /// </summary>
    public static int GetId(this ClaimsPrincipal user)
    {
        var id = user.FindFirstValue(ClaimTypes.NameIdentifier);
        return int.Parse(id);
    }

    /// <summary>
    /// Returns the user role from the claims associated with the authenticated user.
    /// </summary>
    public static UserRole GetRole(this ClaimsPrincipal user)
    {
        var role = user.FindFirstValue(ClaimTypes.Role);

        if (!Enum.TryParse<UserRole>(role, out var userRole))
            throw new UnauthorizedAccessException("Invalid user role.");

        return userRole;
    }
}