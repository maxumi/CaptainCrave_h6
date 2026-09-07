namespace Api.DTOs;

// Data der bruges til at oprette en anmeldelse af en restaurant.
public class CreateReviewDto
{
    public int RestaurantId { get; set; }

    // Vurderingen forventes at være mellem 1 og 5.
    public int Rating { get; set; }
}