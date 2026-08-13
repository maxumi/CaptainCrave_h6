using System.ComponentModel.DataAnnotations;
using Api.Models.Enums;

namespace Api.DTOs;

public class CreateOrderDto : IValidatableObject
{
    [Required]
    public int UserId { get; set; }

    [Required]
    public int RestaurantId { get; set; }

    [Required]
    public DeliveryType DeliveryType { get; set; }

    [MaxLength(500)]
    public string? DeliveryAddress { get; set; }

    [Required]
    [MinLength(1, ErrorMessage = "Cart cannot be empty.")]
    public List<CreateOrderItemDto> Items { get; set; } = [];

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (DeliveryType == DeliveryType.Delivery && string.IsNullOrWhiteSpace(DeliveryAddress))
            yield return new ValidationResult(
                "Delivery address is required when delivery type is Delivery.",
                [nameof(DeliveryAddress)]);
    }
}