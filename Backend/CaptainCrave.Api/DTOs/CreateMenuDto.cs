using System.ComponentModel.DataAnnotations;

namespace Api.DTOs;

// Payload used to create a new menu for a restaurant.
public class CreateMenuDto
{
    // Id of the restaurant the new menu will belong to.
    [Required]
    public int RestaurantId { get; set; }

    // Display name of the menu, e.g. "Lunch Menu".
    [Required]
    [MaxLength(100)]
    public string Name { get; set; } = string.Empty;
}
