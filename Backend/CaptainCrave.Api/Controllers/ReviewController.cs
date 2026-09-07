using Api.DTOs;
using Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Api.Controllers;

// Håndterer requests relateret til anmeldelser og vurderinger af restauranter.
[ApiController]
[Route("api/[controller]")]
public class ReviewsController : ControllerBase
{
    private readonly IReviewService _reviewService;

    public ReviewsController(IReviewService reviewService)
    {
        _reviewService = reviewService;
    }

    // Henter en restaurants gennemsnitlige vurdering og øvrige anmeldelsesoplysninger.
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

    // Henter den aktuelle brugers egen anmeldelse af en restaurant, hvis den findes.
    // GET: api/reviews/restaurant/5/mine
    [HttpGet("restaurant/{restaurantId:int}/mine")]
    [Authorize]
    public async Task<ActionResult<ReviewDto>> GetMine(int restaurantId)
    {
        var userId = User.GetId();

        var review =
            await _reviewService.GetMyReviewAsync(userId, restaurantId);

        if (review == null)
        {
            return NotFound();
        }

        return Ok(review);
    }

    // Opretter en anmeldelse for en restaurant.
    // Servicen kontrollerer blandt andet rating og om kunden har ret til at anmelde restauranten.
    // POST: api/reviews
    [HttpPost]
    [Authorize]
    public async Task<ActionResult<ReviewDto>> Create(
        [FromBody] CreateReviewDto dto)
    {
        var userId = User.GetId();

        try
        {
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
        catch (UnauthorizedAccessException ex)
        {
            return StatusCode(StatusCodes.Status403Forbidden, new { message = ex.Message });
        }
    }

    // Opdaterer en eksisterende anmeldelse, hvis den tilhører den aktuelle bruger.
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