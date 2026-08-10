namespace Api.Models;

// Represents a single item on a restaurant's menu
public class MenuItem
{
    public int Id { get; set; }
    public int RestaurantId { get; set; }
    public int? CategoryId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public string ImageUrl { get; set; } = string.Empty;
    public bool IsAvailable { get; set; } = true;

    // Navigation properties
    public Restaurant Restaurant { get; set; } = null!;
    public Category? Category { get; set; }
}