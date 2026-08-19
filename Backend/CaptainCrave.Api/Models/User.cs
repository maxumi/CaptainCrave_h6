using Api.Models.Enums;
using Microsoft.AspNetCore.Identity;

namespace Api.Models;

// Represents a user stored in the database
public class User : IdentityUser<int>
{
    public string Name { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public double? Latitude { get; set; }
    public double? Longitude { get; set; }
    public UserRole Role { get; set; } = UserRole.Customer;
    public DateTime CreatedAt { get; set; }
}