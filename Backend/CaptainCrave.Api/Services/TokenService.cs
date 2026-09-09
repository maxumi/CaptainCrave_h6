using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Api.Models;
using Microsoft.IdentityModel.Tokens;

namespace Api.Services;

// Genererer et signeret JWT-token til en bruger, ud fra indstillinger i appsettings.json
public class TokenService(IConfiguration config) : ITokenService
{
    /// <summary>
    /// Bygger et JWT-token (en slags digitalt bevis) med brugerens id, e-mail og rolle skrevet
    /// ind i sig, og underskriver det med en hemmelig nøgle, så ingen kan lave et falsk token.
    /// Klienten sender dette token med i alle kald bagefter, så serveren ved hvem der spørger.
    /// </summary>
    /// <returns>Et færdigt, underskrevet JWT-token som en tekststreng.</returns>
    public string GenerateToken(User user)
    {
        // Henter den hemmelige nøgle, der bruges til at signere tokenet.
        var secret = config["Jwt:Secret"]
            ?? throw new InvalidOperationException("JWT secret is not configured.");

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        // Claims indeholder de brugeroplysninger, som senere kan læses fra det autentificerede token.
        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new Claim(JwtRegisteredClaimNames.Email, user.Email ?? string.Empty),
            new Claim(ClaimTypes.Role, user.Role.ToString()),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

        // Henter tokenets levetid fra konfigurationen og bruger 60 minutter som standard.
        var expiryMinutes = config.GetValue<int>("Jwt:ExpiryMinutes", 60);

        // Opretter og signerer JWT-tokenet med issuer, audience, claims og udløbstidspunkt.
        var token = new JwtSecurityToken(
            issuer: config["Jwt:Issuer"],
            audience: config["Jwt:Audience"],
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(expiryMinutes),
            signingCredentials: credentials);

        // Konverterer tokenet til den streng, der sendes tilbage til klienten.
        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
