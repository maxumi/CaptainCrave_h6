using Api.DTOs;
using Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ReviewsController : ControllerBase
{
    private readonly IReviewService _reviewService;

    public ReviewsController(IReviewService reviewService)
    {
        _reviewService = reviewService;
    }

    // Returns reviews and the average rating for a restaurant.
    // GET: api/reviews/restaurant/5
    [HttpGet("restaurant/{restaurantId:int}")]
    [AllowAnonymous]
    public async Task<ActionResult<RestaurantReviewSummaryDto>> GetByRestaurant(
        int restaurantId)
    {
        var result =
            await _reviewService.GetByRestaurantIdAsync(restaurantId);

        return Ok(result);
    }

    // Creates a review for a restaurant.
    // POST: api/reviews
    [HttpPost]
    [Authorize]
    public async Task<ActionResult<ReviewDto>> Create(
        [FromBody] CreateReviewDto dto)
    {
        var userId = User.GetId();

        var review =
            await _reviewService.CreateAsync(userId, dto);

        if (review == null)
        {
            return BadRequest(
                "Review could not be created. Make sure the rating is between 1 and 5 and that you have not already reviewed this restaurant.");
        }

        return CreatedAtAction(
            nameof(GetByRestaurant),
            new { restaurantId = review.RestaurantId },
            review);
    }

    // Updates one of the logged-in user's existing reviews.
    // PUT: api/reviews/5
    [HttpPut("{reviewId:int}")]
    [Authorize]
    public async Task<ActionResult<ReviewDto>> Update(
        int reviewId,
        [FromBody] UpdateReviewDto dto)
    {
        var userId = User.GetId();

        var review =
            await _reviewService.UpdateAsync(
                userId,
                reviewId,
                dto);

        if (review == null)
        {
            return BadRequest(
                "Review could not be updated. Make sure the review belongs to you and the rating is between 1 and 5.");
        }

        return Ok(review);
    }
}