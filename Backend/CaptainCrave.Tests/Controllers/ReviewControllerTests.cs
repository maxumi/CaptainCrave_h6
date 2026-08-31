using Api.Controllers;
using Api.DTOs;
using Api.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using System.Security.Claims;

namespace Api.Tests.Controllers;

// Unit tests for ReviewsController.
// IReviewService is mocked so no database or business-rule logic runs.
public class ReviewControllerTests
{
    // Creates a ReviewsController with a mocked IReviewService and a pre-authenticated user.
    private static (ReviewsController controller, Mock<IReviewService> mockService) CreateController(int userId = 1)
    {
        var mockService = new Mock<IReviewService>();
        var controller = new ReviewsController(mockService.Object);
        var claims = new[] { new Claim(ClaimTypes.NameIdentifier, userId.ToString()) };
        var user = new ClaimsPrincipal(new ClaimsIdentity(claims, "Test"));
        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = user }
        };
        return (controller, mockService);
    }

    [Fact]
    public async Task GetByRestaurant_ReturnsOkWithSummary()
    {
        var (controller, mockService) = CreateController();
        var summary = new RestaurantReviewSummaryDto { RestaurantId = 5, AverageRating = 4.5, ReviewCount = 2 };
        mockService.Setup(s => s.GetByRestaurantIdAsync(5)).ReturnsAsync(summary);

        var result = await controller.GetByRestaurant(5);

        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        Assert.Same(summary, okResult.Value);
    }

    [Fact]
    public async Task GetMine_NoReview_ReturnsNotFound()
    {
        var (controller, mockService) = CreateController(userId: 1);
        mockService.Setup(s => s.GetMyReviewAsync(1, 5)).ReturnsAsync((ReviewDto?)null);

        var result = await controller.GetMine(5);

        Assert.IsType<NotFoundResult>(result.Result);
    }

    [Fact]
    public async Task GetMine_HasReview_ReturnsOkWithReview()
    {
        var (controller, mockService) = CreateController(userId: 1);
        var review = new ReviewDto { Id = 7, UserId = 1, RestaurantId = 5, Rating = 4 };
        mockService.Setup(s => s.GetMyReviewAsync(1, 5)).ReturnsAsync(review);

        var result = await controller.GetMine(5);

        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        Assert.Same(review, okResult.Value);
    }

    [Fact]
    public async Task Create_ServiceReturnsNull_ReturnsBadRequest()
    {
        var (controller, mockService) = CreateController();
        mockService.Setup(s => s.CreateAsync(1, It.IsAny<CreateReviewDto>())).ReturnsAsync((ReviewDto?)null);

        var result = await controller.Create(new CreateReviewDto { RestaurantId = 2, Rating = 5 });

        Assert.IsType<BadRequestObjectResult>(result.Result);
    }

    [Fact]
    public async Task Create_UserHasNotOrdered_ReturnsForbidden()
    {
        var (controller, mockService) = CreateController();
        mockService.Setup(s => s.CreateAsync(1, It.IsAny<CreateReviewDto>()))
            .ThrowsAsync(new UnauthorizedAccessException("You can only review a restaurant after a delivered order from it."));

        var result = await controller.Create(new CreateReviewDto { RestaurantId = 2, Rating = 5 });

        var objectResult = Assert.IsType<ObjectResult>(result.Result);
        Assert.Equal(StatusCodes.Status403Forbidden, objectResult.StatusCode);
    }

    [Fact]
    public async Task Create_Valid_ReturnsCreatedAtAction()
    {
        var (controller, mockService) = CreateController();
        var review = new ReviewDto { Id = 9, UserId = 1, RestaurantId = 2, Rating = 5 };
        mockService.Setup(s => s.CreateAsync(1, It.IsAny<CreateReviewDto>())).ReturnsAsync(review);

        var result = await controller.Create(new CreateReviewDto { RestaurantId = 2, Rating = 5 });

        var createdResult = Assert.IsType<CreatedAtActionResult>(result.Result);
        Assert.Same(review, createdResult.Value);
    }

    [Fact]
    public async Task Update_ServiceReturnsNull_ReturnsBadRequest()
    {
        var (controller, mockService) = CreateController();
        mockService.Setup(s => s.UpdateAsync(1, 10, It.IsAny<UpdateReviewDto>())).ReturnsAsync((ReviewDto?)null);

        var result = await controller.Update(10, new UpdateReviewDto { Rating = 5 });

        Assert.IsType<BadRequestObjectResult>(result.Result);
    }

    [Fact]
    public async Task Update_Valid_ReturnsOk()
    {
        var (controller, mockService) = CreateController();
        var review = new ReviewDto { Id = 10, UserId = 1, RestaurantId = 2, Rating = 5 };
        mockService.Setup(s => s.UpdateAsync(1, 10, It.IsAny<UpdateReviewDto>())).ReturnsAsync(review);

        var result = await controller.Update(10, new UpdateReviewDto { Rating = 5 });

        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        Assert.Same(review, okResult.Value);
    }
}
