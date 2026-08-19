namespace Api.Models;

/// <summary>
/// Represents a menu belonging to a restaurant (e.g. Lunch Menu, Dinner Menu).
/// Groups the restaurant's items and can optionally split them into categories.
/// </summary>
public class Menu : ISoftDeletable, IAuditable
{
    // Primary key.
    public int Id { get; set; }

    // Id of the restaurant that owns this menu.
    public int RestaurantId { get; set; }

    // Display name of the menu (e.g. "Lunch Menu", "Drinks").
    public string Name { get; set; } = string.Empty;

    // Soft delete: hidden from normal queries when true, but still recoverable.
    public bool IsDeleted { get; set; }
    public DateTime? DeletedAt { get; set; }

    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }

    // Navigation properties
    public Restaurant Restaurant { get; set; } = null!;
    public ICollection<Category> Categories { get; set; } = new List<Category>();
    public ICollection<MenuItem> MenuItems { get; set; } = new List<MenuItem>();
}
