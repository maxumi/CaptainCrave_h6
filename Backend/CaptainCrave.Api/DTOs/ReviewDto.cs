namespace Api.DTOs;

// Repræsenterer en anmeldelse, som returneres fra API'et.
public class ReviewDto
{
    public int Id { get; set; }

    public int UserId { get; set; }

    public int RestaurantId { get; set; }

    // Restaurantens vurdering fra 1 til 5 stjerner.
    public int Rating { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }
}