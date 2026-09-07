using System.ComponentModel.DataAnnotations;

namespace Api.DTOs;

// Data der bruges til at oprette en ny menu for en restaurant.
public class CreateMenuDto
{
    // ID på restauranten som menuen skal tilhøre.
    [Required]
    public int RestaurantId { get; set; }

    // Navnet der vises for menuen, f.eks. "Lunch Menu".
    [Required]
    [MaxLength(100)]
    public string Name { get; set; } = string.Empty;
}
