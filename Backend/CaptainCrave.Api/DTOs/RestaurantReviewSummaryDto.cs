namespace Api.DTOs;

public class RestaurantReviewSummaryDto
{
    public int RestaurantId { get; set; }

    public double AverageRating { get; set; }

    public int ReviewCount { get; set; }

    public List<ReviewDto> Reviews { get; set; } = [];
}