using System.ComponentModel.DataAnnotations;

namespace Api.DTOs;

public class CreateOrderItemDto
{
    [Required]
    public int MenuItemId { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "Quantity must be greater than 0.")]
    public int Quantity { get; set; }
}