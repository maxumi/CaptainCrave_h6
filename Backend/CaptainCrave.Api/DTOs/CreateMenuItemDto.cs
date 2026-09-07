using System.ComponentModel.DataAnnotations;

namespace Api.DTOs;

// Data der bruges til at oprette eller opdatere et menu-item.
public class CreateMenuItemDto
{
    [Required]
    public int MenuId { get; set; }

    // Kategorien er valgfri, så et menu-item kan eksistere uden en kategori.
    public int? CategoryId { get; set; }

    [Required]
    [MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(500)]
    public string Description { get; set; } = string.Empty;

    [Range(0.01, 9999)]
    public decimal Price { get; set; }

    [MaxLength(500)]
    public string ImageUrl { get; set; } = string.Empty;

    public bool IsAvailable { get; set; } = true;
}