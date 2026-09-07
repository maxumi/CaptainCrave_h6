using Api.DTOs;
using Api.Models;
using Api.Repositories;
using Api.Services;
using Moq;

namespace Api.Tests.Services;

// Unit-tests for CategoryService's forretningslogik. Ejerskab afgøres via kategoriens
// menu (via IMenuService), så IMenuService bliver mocket sammen med repositories.
public class CategoryServiceTests
{
    /// <summary>Opretter CategoryService og alle dens afhængigheder som mocks.</summary>
    /// <returns>Servicen og de tre mocks, som hver test kan sætte forventninger på.</returns>
    private static (
        CategoryService service,
        Mock<ICategoryRepository> mockCategoryRepository,
        Mock<IMenuService> mockMenuService,
        Mock<IRestaurantRepository> mockRestaurantRepository) CreateService()
    {
        var mockCategoryRepository = new Mock<ICategoryRepository>();
        var mockMenuService = new Mock<IMenuService>();
        var mockRestaurantRepository = new Mock<IRestaurantRepository>();
        var service = new CategoryService(mockCategoryRepository.Object, mockMenuService.Object, mockRestaurantRepository.Object);
        return (service, mockCategoryRepository, mockMenuService, mockRestaurantRepository);
    }

    // Opretter en gyldig kategori, og den får et rigtigt id fra databasen.
    [Fact]
    public async Task CreateAsync_ValidDto_ReturnsCreatedCategory()
    {
        var (service, mockCategoryRepository, _, _) = CreateService();
        mockCategoryRepository.Setup(r => r.CreateAsync(It.IsAny<Category>())).ReturnsAsync((Category c) => { c.Id = 3; return c; });

        var result = await service.CreateAsync(new CreateCategoryDto { MenuId = 1, Name = "Burgers" });

        Assert.Equal(3, result.Id);
    }

    // Hvis kategorien slet ikke findes, kan man ikke slette den, og servicen giver false.
    [Fact]
    public async Task DeleteAsync_CategoryNotFound_ReturnsFalse()
    {
        var (service, mockCategoryRepository, _, _) = CreateService();
        mockCategoryRepository.Setup(r => r.GetByIdAsync(99)).ReturnsAsync((Category?)null);

        var result = await service.DeleteAsync(99, 1, false);

        Assert.False(result);
    }

    // Ejeren af restauranten må slette kategorien, og den bliver soft-deleted.
    [Fact]
    public async Task DeleteAsync_Owner_ReturnsTrue()
    {
        var (service, mockCategoryRepository, mockMenuService, mockRestaurantRepository) = CreateService();
        var category = new Category { Id = 1, MenuId = 20, Name = "Burgers" };
        mockCategoryRepository.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(category);
        mockMenuService.Setup(s => s.GetByIdIncludingDeletedAsync(20)).ReturnsAsync(new MenuDto { Id = 20, RestaurantId = 10 });
        mockRestaurantRepository.Setup(r => r.GetByUserIdAsync(5)).ReturnsAsync([new Restaurant { Id = 10, UserId = 5 }]);

        var result = await service.DeleteAsync(1, 5, false);

        Assert.True(result);
        mockCategoryRepository.Verify(r => r.SoftDeleteAsync(category), Times.Once);
    }

    // Hvis menuen, kategorien hører til, ikke længere findes, kan ejerskab ikke tjekkes, og sletning fejler.
    [Fact]
    public async Task DeleteAsync_MenuNoLongerExists_ReturnsFalse()
    {
        var (service, mockCategoryRepository, mockMenuService, _) = CreateService();
        var category = new Category { Id = 1, MenuId = 20, Name = "Burgers" };
        mockCategoryRepository.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(category);
        mockMenuService.Setup(s => s.GetByIdIncludingDeletedAsync(20)).ReturnsAsync((MenuDto?)null);

        var result = await service.DeleteAsync(1, 5, false);

        Assert.False(result);
    }

    // En bruger, der ikke ejer restauranten, må ikke slette kategorien.
    [Fact]
    public async Task DeleteAsync_NotOwner_ReturnsFalse()
    {
        var (service, mockCategoryRepository, mockMenuService, mockRestaurantRepository) = CreateService();
        var category = new Category { Id = 1, MenuId = 20, Name = "Burgers" };
        mockCategoryRepository.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(category);
        mockMenuService.Setup(s => s.GetByIdIncludingDeletedAsync(20)).ReturnsAsync(new MenuDto { Id = 20, RestaurantId = 10 });
        mockRestaurantRepository.Setup(r => r.GetByUserIdAsync(5)).ReturnsAsync([]);

        var result = await service.DeleteAsync(1, 5, false);

        Assert.False(result);
    }

    // Man kan ikke gendanne en kategori, der slet ikke er slettet.
    [Fact]
    public async Task RestoreAsync_NotDeleted_ReturnsFalse()
    {
        var (service, mockCategoryRepository, _, _) = CreateService();
        mockCategoryRepository.Setup(r => r.GetByIdIncludingDeletedAsync(1)).ReturnsAsync(new Category { Id = 1, MenuId = 20, IsDeleted = false });

        var result = await service.RestoreAsync(1, 5, false);

        Assert.False(result);
    }

    // En admin må slette kategorien for altid uden at skulle eje restauranten.
    [Fact]
    public async Task HardDeleteAsync_Admin_BypassesOwnershipCheck()
    {
        var (service, mockCategoryRepository, mockMenuService, _) = CreateService();
        var category = new Category { Id = 1, MenuId = 20 };
        mockCategoryRepository.Setup(r => r.GetByIdIncludingDeletedAsync(1)).ReturnsAsync(category);

        var result = await service.HardDeleteAsync(1, 999, true);

        Assert.True(result);
        mockCategoryRepository.Verify(r => r.HardDeleteAsync(category), Times.Once);
        mockMenuService.Verify(s => s.GetByIdIncludingDeletedAsync(It.IsAny<int>()), Times.Never);
    }

    // En bruger, der ikke ejer restauranten, må ikke se dens slettede kategorier.
    [Fact]
    public async Task GetDeletedByRestaurantIdAsync_NotOwner_ReturnsNull()
    {
        var (service, _, _, mockRestaurantRepository) = CreateService();
        mockRestaurantRepository.Setup(r => r.GetByUserIdAsync(5)).ReturnsAsync([]);

        var result = await service.GetDeletedByRestaurantIdAsync(10, 5, false);

        Assert.Null(result);
    }
}
