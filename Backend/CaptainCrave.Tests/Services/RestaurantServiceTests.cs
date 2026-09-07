using Api.DTOs;
using Api.Models;
using Api.Repositories;
using Api.Services;
using Moq;

namespace Api.Tests.Services;

// Unit-tests for RestaurantService's forretningslogik (oprettelse, søgning i nærheden, soft delete, ejerskab).
public class RestaurantServiceTests
{
    /// <summary>Opretter RestaurantService med database-, billed- og anmeldelsesafhængigheder som mocks.</summary>
    /// <returns>Servicen og alle tre mocks, så hver test kan bestemme de underliggende svar.</returns>
    private static (RestaurantService service, Mock<IRestaurantRepository> mockRepository, Mock<IImageStorageService> mockImageStorageService, Mock<IReviewRepository> mockReviewRepository) CreateService()
    {
        var mockRepository = new Mock<IRestaurantRepository>();
        var mockImageStorageService = new Mock<IImageStorageService>();
        var mockReviewRepository = new Mock<IReviewRepository>();

        // Standard er "ingen anmeldelser", så tests der ikke bekymrer sig om vurderinger ikke selv skal opstille dette.
        mockReviewRepository.Setup(r => r.GetSummaryAsync(It.IsAny<int>())).ReturnsAsync((0, 0));
        mockReviewRepository.Setup(r => r.GetSummariesByRestaurantIdsAsync(It.IsAny<IEnumerable<int>>()))
            .ReturnsAsync(new Dictionary<int, (double AverageRating, int ReviewCount)>());

        var service = new RestaurantService(mockRepository.Object, mockImageStorageService.Object, mockReviewRepository.Object);
        return (service, mockRepository, mockImageStorageService, mockReviewRepository);
    }

    // Opretter en gyldig restaurant, og den får et rigtigt id fra databasen.
    [Fact]
    public async Task CreateAsync_ValidDto_ReturnsCreatedRestaurant()
    {
        var (service, mockRepository, _, _) = CreateService();
        mockRepository.Setup(r => r.CreateAsync(It.IsAny<Restaurant>())).ReturnsAsync((Restaurant r) => { r.Id = 7; return r; });

        var result = await service.CreateAsync(new CreateRestaurantDto { UserId = 1, Name = "Burger Place", Address = "Main St" });

        Assert.Equal(7, result.Id);
    }

    // Restaurantens gennemsnitlige vurdering og antal anmeldelser bliver hængt på svaret.
    [Fact]
    public async Task GetByIdAsync_RestaurantHasReviews_AttachesAverageRatingAndCount()
    {
        var (service, mockRepository, _, mockReviewRepository) = CreateService();
        mockRepository.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(new Restaurant { Id = 1, Name = "Burger Place" });
        mockReviewRepository.Setup(r => r.GetSummaryAsync(1)).ReturnsAsync((4.5, 2));

        var result = await service.GetByIdAsync(1);

        Assert.Equal(4.5, result?.AverageRating);
        Assert.Equal(2, result?.ReviewCount);
    }

    // Når man henter alle restauranter, får hver af dem sin egen vurdering hængt på.
    [Fact]
    public async Task GetAllAsync_AttachesRatingSummaryPerRestaurant()
    {
        var (service, mockRepository, _, mockReviewRepository) = CreateService();
        mockRepository.Setup(r => r.GetAllAsync()).ReturnsAsync(
        [
            new Restaurant { Id = 1, Name = "Rated" },
            new Restaurant { Id = 2, Name = "Unrated" }
        ]);
        mockReviewRepository.Setup(r => r.GetSummariesByRestaurantIdsAsync(It.IsAny<IEnumerable<int>>()))
            .ReturnsAsync(new Dictionary<int, (double AverageRating, int ReviewCount)>
            {
                [1] = (5, 3)
            });

        var result = (await service.GetAllAsync()).ToList();

        Assert.Equal(5, result.Single(r => r.Id == 1).AverageRating);
        Assert.Equal(3, result.Single(r => r.Id == 1).ReviewCount);
        Assert.Equal(0, result.Single(r => r.Id == 2).AverageRating);
    }

    // Kun restauranter inden for radiussen kommer med i søgningen i nærheden.
    [Fact]
    public async Task GetNearbyRestaurantsAsync_OnlyReturnsRestaurantsWithinRadius()
    {
        var (service, mockRepository, _, _) = CreateService();
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

    // Hvis restauranten slet ikke findes, kan man ikke slette den.
    [Fact]
    public async Task DeleteAsync_RestaurantNotFound_ReturnsFalse()
    {
        var (service, mockRepository, _, _) = CreateService();
        mockRepository.Setup(r => r.GetByIdAsync(99)).ReturnsAsync((Restaurant?)null);

        var result = await service.DeleteAsync(99, 1, false);

        Assert.False(result);
    }

    // Ejeren må slette sin restaurant, og den bliver soft-deleted.
    [Fact]
    public async Task DeleteAsync_Owner_ReturnsTrue()
    {
        var (service, mockRepository, _, _) = CreateService();
        var restaurant = new Restaurant { Id = 1, UserId = 5 };
        mockRepository.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(restaurant);

        var result = await service.DeleteAsync(1, 5, false);

        Assert.True(result);
        mockRepository.Verify(r => r.SoftDeleteAsync(restaurant), Times.Once);
    }

    // En bruger, der ikke ejer restauranten, må ikke slette den.
    [Fact]
    public async Task DeleteAsync_NotOwner_ReturnsFalse()
    {
        var (service, mockRepository, _, _) = CreateService();
        var restaurant = new Restaurant { Id = 1, UserId = 5 };
        mockRepository.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(restaurant);

        var result = await service.DeleteAsync(1, 999, false);

        Assert.False(result);
        mockRepository.Verify(r => r.SoftDeleteAsync(It.IsAny<Restaurant>()), Times.Never);
    }

    // Man kan ikke gendanne en restaurant, der slet ikke er slettet.
    [Fact]
    public async Task RestoreAsync_NotDeleted_ReturnsFalse()
    {
        var (service, mockRepository, _, _) = CreateService();
        mockRepository.Setup(r => r.GetByIdIncludingDeletedAsync(1)).ReturnsAsync(new Restaurant { Id = 1, UserId = 5, IsDeleted = false });

        var result = await service.RestoreAsync(1, 5, false);

        Assert.False(result);
    }

    // En admin må slette enhver restaurant for altid uden at eje den.
    [Fact]
    public async Task HardDeleteAsync_Admin_BypassesOwnershipCheck()
    {
        var (service, mockRepository, _, _) = CreateService();
        var restaurant = new Restaurant { Id = 1, UserId = 5 };
        mockRepository.Setup(r => r.GetByIdIncludingDeletedAsync(1)).ReturnsAsync(restaurant);

        var result = await service.HardDeleteAsync(1, 999, true);

        Assert.True(result);
        mockRepository.Verify(r => r.HardDeleteAsync(restaurant), Times.Once);
    }

    // Henter alle slettede restauranter.
    [Fact]
    public async Task GetDeletedAsync_ReturnsAllSoftDeletedRestaurants()
    {
        var (service, mockRepository, _, _) = CreateService();
        mockRepository.Setup(r => r.GetDeletedAsync()).ReturnsAsync(
        [
            new Restaurant { Id = 1, IsDeleted = true },
            new Restaurant { Id = 2, IsDeleted = true }
        ]);

        var result = await service.GetDeletedAsync();

        Assert.Equal(2, result.Count());
    }

    // UpdateImageUrlAsync

    // Hvis restauranten ikke findes, sker der intet, og ingen fil bliver slettet.
    [Fact]
    public async Task UpdateImageUrlAsync_RestaurantNotFound_ReturnsNull()
    {
        var (service, mockRepository, mockImageStorageService, _) = CreateService();
        mockRepository.Setup(r => r.GetByIdAsync(99)).ReturnsAsync((Restaurant?)null);

        var result = await service.UpdateImageUrlAsync(99, "/uploads/restaurants/new.jpg", 1, false);

        Assert.Null(result);
        mockImageStorageService.Verify(s => s.Delete(It.IsAny<string>()), Times.Never);
    }

    // En bruger, der ikke ejer restauranten, må ikke skifte dens billede.
    [Fact]
    public async Task UpdateImageUrlAsync_NotOwner_ReturnsNullAndDoesNotUpdate()
    {
        var (service, mockRepository, _, _) = CreateService();
        var restaurant = new Restaurant { Id = 1, UserId = 5, ImageUrl = "/uploads/restaurants/old.jpg" };
        mockRepository.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(restaurant);

        var result = await service.UpdateImageUrlAsync(1, "/uploads/restaurants/new.jpg", 999, false);

        Assert.Null(result);
        mockRepository.Verify(r => r.UpdateAsync(It.IsAny<Restaurant>()), Times.Never);
    }

    // Ejeren må skifte billede, og det gamle billede bliver slettet fra disken.
    [Fact]
    public async Task UpdateImageUrlAsync_Owner_UpdatesImageAndDeletesPreviousFile()
    {
        var (service, mockRepository, mockImageStorageService, _) = CreateService();
        var restaurant = new Restaurant { Id = 1, UserId = 5, ImageUrl = "/uploads/restaurants/old.jpg" };
        mockRepository.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(restaurant);
        mockRepository.Setup(r => r.UpdateAsync(restaurant)).ReturnsAsync(restaurant);

        var result = await service.UpdateImageUrlAsync(1, "/uploads/restaurants/new.jpg", 5, false);

        Assert.Equal("/uploads/restaurants/new.jpg", result?.ImageUrl);
        mockImageStorageService.Verify(s => s.Delete("/uploads/restaurants/old.jpg"), Times.Once);
    }

    // En admin må skifte billede uden selv at eje restauranten.
    [Fact]
    public async Task UpdateImageUrlAsync_Admin_BypassesOwnershipCheck()
    {
        var (service, mockRepository, _, _) = CreateService();
        var restaurant = new Restaurant { Id = 1, UserId = 5, ImageUrl = "/uploads/restaurants/old.jpg" };
        mockRepository.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(restaurant);
        mockRepository.Setup(r => r.UpdateAsync(restaurant)).ReturnsAsync(restaurant);

        var result = await service.UpdateImageUrlAsync(1, "/uploads/restaurants/new.jpg", 999, true);

        Assert.Equal("/uploads/restaurants/new.jpg", result?.ImageUrl);
    }
}
