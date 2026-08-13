using System.ComponentModel.DataAnnotations;
using Api.Models.Enums;

namespace Api.DTOs.Auth;

// The data a user sends when creating a new account
public class RegisterRequestDto
{
    [Required]
    public string Name { get; set; } = string.Empty;

    [Required]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Required]
    [MinLength(8)]
    public string Password { get; set; } = string.Empty;

    public string Address { get; set; } = string.Empty;

    [Range(-90, 90)]
    public double? Latitude { get; set; }

    [Range(-180, 180)]
    public double? Longitude { get; set; }

    public UserRole Role { get; set; } = UserRole.Customer;
}
