namespace Api.Models;

// Represents a single item on a menu, optionally grouped under a category
public class MenuItem : ISoftDeletable, IAuditable
{
    public int Id { get; set; }
    public int MenuId { get; set; }
    public int? CategoryId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public string ImageUrl { get; set; } = string.Empty;
    public bool IsAvailable { get; set; } = true;

    // Soft delete: hidden from normal queries when true, but still recoverable.
    public bool IsDeleted { get; set; }
    public DateTime? DeletedAt { get; set; }

    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }

    // Navigation properties
    public Menu Menu { get; set; } = null!;
    public Category? Category { get; set; }
}