namespace Api.Models;

// Represents a restaurant registered on the platform
public class Restaurant : ISoftDeletable, IAuditable
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public double Latitude { get; set; }
    public double Longitude { get; set; }
    public string ImageUrl { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }

    // Soft delete: hidden from normal queries when true, but still recoverable.
    public bool IsDeleted { get; set; }
    public DateTime? DeletedAt { get; set; }

    // Navigation properties
    public User User { get; set; } = null!;
    public ICollection<Menu> Menus { get; set; } = new List<Menu>();

    public ICollection<Review> Reviews { get; set; } = [];
}