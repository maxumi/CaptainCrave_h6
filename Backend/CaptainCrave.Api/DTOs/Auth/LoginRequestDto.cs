using System.ComponentModel.DataAnnotations;

namespace Api.DTOs.Auth;

// The data a user sends when logging in
public class LoginRequestDto
{
    [Required]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Required]
    public string Password { get; set; } = string.Empty;
}
