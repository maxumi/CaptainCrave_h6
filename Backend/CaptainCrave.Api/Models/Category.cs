namespace Api.Models;

// Represents a category used to optionally group menu items within a menu (e.g. Burgers, Drinks)
public class Category
{
    public int Id { get; set; }
    public int MenuId { get; set; }
    public string Name { get; set; } = string.Empty;

    // Navigation properties
    public Menu Menu { get; set; } = null!;
    public ICollection<MenuItem> MenuItems { get; set; } = new List<MenuItem>();
}
