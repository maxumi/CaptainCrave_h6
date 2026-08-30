namespace Api.Models;

public class Review
{
    public int Id { get; set; }

    // Customer who created the review
    public int UserId { get; set; }
    public User User { get; set; } = null!;

    // Restaurant being reviewed
    public int RestaurantId { get; set; }
    public Restaurant Restaurant { get; set; } = null!;

    // Rating from 1 to 5
    public int Rating { get; set; }

    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}