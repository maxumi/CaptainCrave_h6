using System.ComponentModel.DataAnnotations;
using Api.Models.Enums;

namespace Api.DTOs;

// Data der bruges til at ændre status på en eksisterende ordre.
public class UpdateOrderStatusDto
{
    [Required]
    public OrderStatus Status { get; set; }
}
