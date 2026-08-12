namespace Api.Models;

// Represents a menu category belonging to a restaurant (e.g. Burgers, Drinks)
public class Category
{
    public int Id { get; set; }
    public int RestaurantId { get; set; }
    public string Name { get; set; } = string.Empty;

    // Navigation properties
    public Restaurant Restaurant { get; set; } = null!;
    public ICollection<MenuItem> MenuItems { get; set; } = new List<MenuItem>();
}
