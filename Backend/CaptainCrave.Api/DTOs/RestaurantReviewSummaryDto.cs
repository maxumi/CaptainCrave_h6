namespace Api.DTOs;

// Opsummerer en restaurants vurderinger.
// Indeholder ikke individuelle anmeldelser, men kun gennemsnit og antal.
public class RestaurantReviewSummaryDto
{
    public int RestaurantId { get; set; }

    public double AverageRating { get; set; }

    public int ReviewCount { get; set; }
}