namespace Api.Models;

// Represents a menu belonging to a restaurant (e.g. Lunch Menu, Dinner Menu)
public class Menu
{
    public int Id { get; set; }
    public int RestaurantId { get; set; }
    public string Name { get; set; } = string.Empty;

    // Navigation properties
    public Restaurant Restaurant { get; set; } = null!;
    public ICollection<Category> Categories { get; set; } = new List<Category>();
    public ICollection<MenuItem> MenuItems { get; set; } = new List<MenuItem>();
}
