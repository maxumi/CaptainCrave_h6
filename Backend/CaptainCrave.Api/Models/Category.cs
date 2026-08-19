namespace Api.Models;

// Represents a category used to optionally group menu items within a menu (e.g. Burgers, Drinks)
public class Category : ISoftDeletable, IAuditable
{
    public int Id { get; set; }
    public int MenuId { get; set; }
    public string Name { get; set; } = string.Empty;

    // Soft delete: hidden from normal queries when true, but still recoverable.
    public bool IsDeleted { get; set; }
    public DateTime? DeletedAt { get; set; }

    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }

    // Navigation properties
    public Menu Menu { get; set; } = null!;
    public ICollection<MenuItem> MenuItems { get; set; } = new List<MenuItem>();
}
