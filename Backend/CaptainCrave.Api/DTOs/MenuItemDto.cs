namespace Api.DTOs;

// Repræsenterer et menu-item, som returneres fra API'et.
public class MenuItemDto
{
    public int Id { get; set; }
    public int MenuId { get; set; }
    public int? CategoryId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public string ImageUrl { get; set; } = string.Empty;
    public bool IsAvailable { get; set; }

    // Angiver om menu-item'et er soft-deleted og derfor skjult, men stadig kan gendannes.
    public bool IsDeleted { get; set; }
    public DateTime? DeletedAt { get; set; }

    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}
