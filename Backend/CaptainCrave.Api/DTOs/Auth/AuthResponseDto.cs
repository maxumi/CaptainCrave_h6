namespace Api.DTOs.Auth;

// Data der sendes tilbage til klienten efter vellykket registrering eller login.
public class AuthResponseDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public double? Latitude { get; set; }
    public double? Longitude { get; set; }
    public string Role { get; set; } = string.Empty;

    // JWT-token der bruges til autentificerede requests.
    public string Token { get; set; } = string.Empty;
}
