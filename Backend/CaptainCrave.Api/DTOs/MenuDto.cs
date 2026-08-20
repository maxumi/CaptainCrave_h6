namespace Api.DTOs;

// Read-only representation of a menu returned by the API.
public class MenuDto
{
    // Menu's unique identifier.
    public int Id { get; set; }

    // Id of the restaurant this menu belongs to.
    public int RestaurantId { get; set; }

    // Display name of the menu.
    public string Name { get; set; } = string.Empty;

    // Whether the menu is soft-deleted (hidden but recoverable).
    public bool IsDeleted { get; set; }
    public DateTime? DeletedAt { get; set; }

    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}
