namespace Api.DTOs;

// Summarizes a restaurant's ratings. Deliberately excludes individual reviews —
// use IReviewService.GetMyReviewAsync for the current user's own review.
public class RestaurantReviewSummaryDto
{
    public int RestaurantId { get; set; }

    public double AverageRating { get; set; }

    public int ReviewCount { get; set; }
}