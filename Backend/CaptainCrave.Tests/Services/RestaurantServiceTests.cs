using Api.DTOs;
using Api.Models;
using Api.Repositories;
using Api.Services;
using Moq;

namespace Api.Tests.Services;

// Unit tests for RestaurantService business logic (creation, nearby search, soft delete, ownership).
public class RestaurantServiceTests
{
    private static (RestaurantService service, Mock<IRestaurantRepository> mockRepository) CreateService()
    {
        var mockRepository = new Mock<IRestaurantRepository>();
        var service = new RestaurantService(mockRepository.Object);
        return (service, mockRepository);
    }

    [Fact]
    public async Task CreateAsync_ValidDto_ReturnsCreatedRestaurant()
    {
        var (service, mockRepository) = CreateService();
        mockRepository.Setup(r => r.CreateAsync(It.IsAny<Restaurant>())).ReturnsAsync((Restaurant r) => { r.Id = 7; return r; });

        var result = await service.CreateAsync(new CreateRestaurantDto { UserId = 1, Name = "Burger Place", Address = "Main St" });

        Assert.Equal(7, result.Id);
    }

    [Fact]
    public async Task GetNearbyRestaurantsAsync_OnlyReturnsRestaurantsWithinRadius()
    {
        var (service, mockRepository) = CreateService();
        mockRepository.Setup(r => r.GetAllAsync()).ReturnsAsync(
        [
            new Restaurant { Id = 1, Name = "Close By", Latitude = 55.6761, Longitude = 12.5683 }, // Copenhagen
            new Restaurant { Id = 2, Name = "Far Away", Latitude = 56.1629, Longitude = 10.2039 }  // Aarhus, ~160km away
        ]);

        // Search from Copenhagen with a 10km radius: only the close restaurant should match.
        var result = await service.GetNearbyRestaurantsAsync(55.6761, 12.5683, 10);

        Assert.Single(result);
        Assert.Equal("Close By", result.First().Name);
    }

    [Fact]
    public async Task DeleteAsync_RestaurantNotFound_ReturnsFalse()
    {
        var (service, mockRepository) = CreateService();
        mockRepository.Setup(r => r.GetByIdAsync(99)).ReturnsAsync((Restaurant?)null);

        var result = await service.DeleteAsync(99, 1, false);

        Assert.False(result);
    }

    [Fact]
    public async Task DeleteAsync_Owner_ReturnsTrue()
    {
        var (service, mockRepository) = CreateService();
        var restaurant = new Restaurant { Id = 1, UserId = 5 };
        mockRepository.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(restaurant);

        var result = await service.DeleteAsync(1, 5, false);

        Assert.True(result);
        mockRepository.Verify(r => r.SoftDeleteAsync(restaurant), Times.Once);
    }

    [Fact]
    public async Task DeleteAsync_NotOwner_ReturnsFalse()
    {
        var (service, mockRepository) = CreateService();
        var restaurant = new Restaurant { Id = 1, UserId = 5 };
        mockRepository.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(restaurant);

        var result = await service.DeleteAsync(1, 999, false);

        Assert.False(result);
        mockRepository.Verify(r => r.SoftDeleteAsync(It.IsAny<Restaurant>()), Times.Never);
    }

    [Fact]
    public async Task RestoreAsync_NotDeleted_ReturnsFalse()
    {
        var (service, mockRepository) = CreateService();
        mockRepository.Setup(r => r.GetByIdIncludingDeletedAsync(1)).ReturnsAsync(new Restaurant { Id = 1, UserId = 5, IsDeleted = false });

        var result = await service.RestoreAsync(1, 5, false);

        Assert.False(result);
    }

    [Fact]
    public async Task HardDeleteAsync_Admin_BypassesOwnershipCheck()
    {
        var (service, mockRepository) = CreateService();
        var restaurant = new Restaurant { Id = 1, UserId = 5 };
        mockRepository.Setup(r => r.GetByIdIncludingDeletedAsync(1)).ReturnsAsync(restaurant);

        var result = await service.HardDeleteAsync(1, 999, true);

        Assert.True(result);
        mockRepository.Verify(r => r.HardDeleteAsync(restaurant), Times.Once);
    }

    [Fact]
    public async Task GetDeletedAsync_ReturnsAllSoftDeletedRestaurants()
    {
        var (service, mockRepository) = CreateService();
        mockRepository.Setup(r => r.GetDeletedAsync()).ReturnsAsync(
        [
            new Restaurant { Id = 1, IsDeleted = true },
            new Restaurant { Id = 2, IsDeleted = true }
        ]);

        var result = await service.GetDeletedAsync();

        Assert.Equal(2, result.Count());
    }
}
