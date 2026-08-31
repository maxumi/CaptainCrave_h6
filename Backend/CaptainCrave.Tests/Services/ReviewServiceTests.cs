using Api.DTOs;
using Api.Models;
using Api.Repositories;
using Api.Services;
using Moq;

namespace Api.Tests.Services;

// Unit tests for ReviewService business logic (rating validation, duplicate prevention,
// review-eligibility enforcement, and rating summaries). Repositories are mocked so no database access occurs.
public class ReviewServiceTests
{
    private static (ReviewService service, Mock<IReviewRepository> mockReviewRepository, Mock<IOrderRepository> mockOrderRepository) CreateService()
    {
        var mockReviewRepository = new Mock<IReviewRepository>();
        var mockOrderRepository = new Mock<IOrderRepository>();
        var service = new ReviewService(mockReviewRepository.Object, mockOrderRepository.Object);
        return (service, mockReviewRepository, mockOrderRepository);
    }

    // CreateAsync

    [Theory]
    [InlineData(0)]
    [InlineData(6)]
    public async Task CreateAsync_RatingOutOfRange_ReturnsNull(int rating)
    {
        var (service, _, _) = CreateService();

        var result = await service.CreateAsync(1, new CreateReviewDto { RestaurantId = 2, Rating = rating });

        Assert.Null(result);
    }

    [Fact]
    public async Task CreateAsync_AlreadyReviewed_ReturnsNull()
    {
        var (service, mockReviewRepository, mockOrderRepository) = CreateService();
        mockReviewRepository.Setup(r => r.GetByUserAndRestaurantAsync(1, 2))
            .ReturnsAsync(new Review { Id = 10, UserId = 1, RestaurantId = 2, Rating = 4 });

        var result = await service.CreateAsync(1, new CreateReviewDto { RestaurantId = 2, Rating = 5 });

        Assert.Null(result);
        mockOrderRepository.Verify(r => r.HasUserOrderedFromRestaurantAsync(It.IsAny<int>(), It.IsAny<int>()), Times.Never);
    }

    [Fact]
    public async Task CreateAsync_UserHasNotOrderedFromRestaurant_ThrowsUnauthorizedAccessException()
    {
        var (service, mockReviewRepository, mockOrderRepository) = CreateService();
        mockReviewRepository.Setup(r => r.GetByUserAndRestaurantAsync(1, 2)).ReturnsAsync((Review?)null);
        mockOrderRepository.Setup(r => r.HasUserOrderedFromRestaurantAsync(1, 2)).ReturnsAsync(false);

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            service.CreateAsync(1, new CreateReviewDto { RestaurantId = 2, Rating = 5 }));
    }

    [Fact]
    public async Task CreateAsync_EligibleCustomer_CreatesAndReturnsReview()
    {
        var (service, mockReviewRepository, mockOrderRepository) = CreateService();
        mockReviewRepository.Setup(r => r.GetByUserAndRestaurantAsync(1, 2)).ReturnsAsync((Review?)null);
        mockOrderRepository.Setup(r => r.HasUserOrderedFromRestaurantAsync(1, 2)).ReturnsAsync(true);
        mockReviewRepository.Setup(r => r.CreateAsync(It.IsAny<Review>()))
            .ReturnsAsync((Review r) => { r.Id = 99; return r; });

        var result = await service.CreateAsync(1, new CreateReviewDto { RestaurantId = 2, Rating = 5 });

        Assert.NotNull(result);
        Assert.Equal(99, result!.Id);
        Assert.Equal(5, result.Rating);
        Assert.Equal(2, result.RestaurantId);
    }

    // UpdateAsync

    [Theory]
    [InlineData(0)]
    [InlineData(6)]
    public async Task UpdateAsync_RatingOutOfRange_ReturnsNull(int rating)
    {
        var (service, _, _) = CreateService();

        var result = await service.UpdateAsync(1, 10, new UpdateReviewDto { Rating = rating });

        Assert.Null(result);
    }

    [Fact]
    public async Task UpdateAsync_ReviewNotFound_ReturnsNull()
    {
        var (service, mockReviewRepository, _) = CreateService();
        mockReviewRepository.Setup(r => r.GetByIdAsync(10)).ReturnsAsync((Review?)null);

        var result = await service.UpdateAsync(1, 10, new UpdateReviewDto { Rating = 4 });

        Assert.Null(result);
    }

    [Fact]
    public async Task UpdateAsync_NotOwner_ReturnsNull()
    {
        var (service, mockReviewRepository, _) = CreateService();
        mockReviewRepository.Setup(r => r.GetByIdAsync(10)).ReturnsAsync(new Review { Id = 10, UserId = 2, RestaurantId = 5, Rating = 3 });

        var result = await service.UpdateAsync(1, 10, new UpdateReviewDto { Rating = 4 });

        Assert.Null(result);
    }

    [Fact]
    public async Task UpdateAsync_Owner_UpdatesRating()
    {
        var (service, mockReviewRepository, _) = CreateService();
        var review = new Review { Id = 10, UserId = 1, RestaurantId = 5, Rating = 3 };
        mockReviewRepository.Setup(r => r.GetByIdAsync(10)).ReturnsAsync(review);
        mockReviewRepository.Setup(r => r.UpdateAsync(review)).ReturnsAsync(review);

        var result = await service.UpdateAsync(1, 10, new UpdateReviewDto { Rating = 4 });

        Assert.Equal(4, result?.Rating);
    }

    // GetByRestaurantIdAsync

    [Fact]
    public async Task GetByRestaurantIdAsync_ReturnsAggregatedSummaryWithoutIndividualReviews()
    {
        var (service, mockReviewRepository, _) = CreateService();
        mockReviewRepository.Setup(r => r.GetSummaryAsync(5)).ReturnsAsync((4.25, 4));

        var result = await service.GetByRestaurantIdAsync(5);

        Assert.Equal(5, result.RestaurantId);
        Assert.Equal(4.25, result.AverageRating);
        Assert.Equal(4, result.ReviewCount);
    }

    [Fact]
    public async Task GetByRestaurantIdAsync_NoReviews_ReturnsZeroSummary()
    {
        var (service, mockReviewRepository, _) = CreateService();
        mockReviewRepository.Setup(r => r.GetSummaryAsync(5)).ReturnsAsync((0, 0));

        var result = await service.GetByRestaurantIdAsync(5);

        Assert.Equal(0, result.AverageRating);
        Assert.Equal(0, result.ReviewCount);
    }

    // GetMyReviewAsync

    [Fact]
    public async Task GetMyReviewAsync_NoReview_ReturnsNull()
    {
        var (service, mockReviewRepository, _) = CreateService();
        mockReviewRepository.Setup(r => r.GetByUserAndRestaurantAsync(1, 5)).ReturnsAsync((Review?)null);

        var result = await service.GetMyReviewAsync(1, 5);

        Assert.Null(result);
    }

    [Fact]
    public async Task GetMyReviewAsync_HasReview_ReturnsDto()
    {
        var (service, mockReviewRepository, _) = CreateService();
        mockReviewRepository.Setup(r => r.GetByUserAndRestaurantAsync(1, 5))
            .ReturnsAsync(new Review { Id = 7, UserId = 1, RestaurantId = 5, Rating = 3 });

        var result = await service.GetMyReviewAsync(1, 5);

        Assert.Equal(7, result?.Id);
        Assert.Equal(3, result?.Rating);
    }
}
