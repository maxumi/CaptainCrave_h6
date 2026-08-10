using System.ComponentModel.DataAnnotations;
using Api.Models.Enums;

namespace Api.DTOs;

public class UpdateOrderStatusDto
{
    [Required]
    public OrderStatus Status { get; set; }
}
