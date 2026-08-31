namespace Api.DTOs;

public class ReviewDto
{
    public int Id { get; set; }

    public int UserId { get; set; }

    public int RestaurantId { get; set; }

    public int Rating { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }
}