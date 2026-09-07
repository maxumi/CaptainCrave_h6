using System.ComponentModel.DataAnnotations;

namespace Api.DTOs;

// Repræsenterer en enkelt vare, der skal tilføjes til en ny ordre.
public class CreateOrderItemDto
{
    [Required]
    public int MenuItemId { get; set; }

    // Antallet skal være mindst 1.
    [Range(1, int.MaxValue, ErrorMessage = "Quantity must be greater than 0.")]
    public int Quantity { get; set; }
}