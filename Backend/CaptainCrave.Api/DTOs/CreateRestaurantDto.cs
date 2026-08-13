using System.ComponentModel.DataAnnotations;

namespace Api.DTOs;

public class CreateRestaurantDto
{
    [Required]
    public int UserId { get; set; }

    [Required]
    [MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(1000)]
    public string Description { get; set; } = string.Empty;

    [Required]
    [MaxLength(255)]
    public string Address { get; set; } = string.Empty;

    [Range(-90, 90)]
    public double Latitude { get; set; }

    [Range(-180, 180)]
    public double Longitude { get; set; }

    [MaxLength(500)]
    public string ImageUrl { get; set; } = string.Empty;

    public bool IsActive { get; set; } = true;
}