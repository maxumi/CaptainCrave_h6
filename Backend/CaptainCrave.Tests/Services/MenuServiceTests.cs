using Api.DTOs;
using Api.Models;
using Api.Repositories;
using Api.Services;
using Moq;

namespace Api.Tests.Services;

// Unit tests for MenuService business logic (creation, soft delete, restore, hard delete, ownership checks).
public class MenuServiceTests
{
    private static (MenuService service, Mock<IMenuRepository> mockMenuRepository, Mock<IRestaurantRepository> mockRestaurantRepository) CreateService()
    {
        var mockMenuRepository = new Mock<IMenuRepository>();
        var mockRestaurantRepository = new Mock<IRestaurantRepository>();
        var service = new MenuService(mockMenuRepository.Object, mockRestaurantRepository.Object);
        return (service, mockMenuRepository, mockRestaurantRepository);
    }

    [Fact]
    public async Task CreateAsync_ValidDto_ReturnsCreatedMenu()
    {
        var (service, mockMenuRepository, _) = CreateService();
        mockMenuRepository.Setup(r => r.CreateAsync(It.IsAny<Menu>())).ReturnsAsync((Menu m) => { m.Id = 5; return m; });

        var result = await service.CreateAsync(new CreateMenuDto { RestaurantId = 1, Name = "Lunch Menu" });

        Assert.Equal(5, result.Id);
        Assert.Equal("Lunch Menu", result.Name);
    }

    [Fact]
    public async Task DeleteAsync_MenuNotFound_ReturnsFalse()
    {
        var (service, mockMenuRepository, _) = CreateService();
        mockMenuRepository.Setup(r => r.GetByIdAsync(99)).ReturnsAsync((Menu?)null);

        var result = await service.DeleteAsync(99, 1, false);

        Assert.False(result);
    }

    [Fact]
    public async Task DeleteAsync_Owner_ReturnsTrueAndSoftDeletes()
    {
        var (service, mockMenuRepository, mockRestaurantRepository) = CreateService();
        var menu = new Menu { Id = 1, RestaurantId = 10, Name = "Lunch" };
        mockMenuRepository.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(menu);
        mockRestaurantRepository.Setup(r => r.GetByUserIdAsync(5)).ReturnsAsync([new Restaurant { Id = 10, UserId = 5 }]);

        var result = await service.DeleteAsync(1, 5, false);

        Assert.True(result);
        mockMenuRepository.Verify(r => r.SoftDeleteAsync(menu), Times.Once);
    }

    [Fact]
    public async Task DeleteAsync_NotOwner_ReturnsFalseAndDoesNotDelete()
    {
        var (service, mockMenuRepository, mockRestaurantRepository) = CreateService();
        var menu = new Menu { Id = 1, RestaurantId = 10, Name = "Lunch" };
        mockMenuRepository.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(menu);
        mockRestaurantRepository.Setup(r => r.GetByUserIdAsync(5)).ReturnsAsync([]);

        var result = await service.DeleteAsync(1, 5, false);

        Assert.False(result);
        mockMenuRepository.Verify(r => r.SoftDeleteAsync(It.IsAny<Menu>()), Times.Never);
    }

    [Fact]
    public async Task DeleteAsync_Admin_BypassesOwnershipCheck()
    {
        var (service, mockMenuRepository, _) = CreateService();
        var menu = new Menu { Id = 1, RestaurantId = 10, Name = "Lunch" };
        mockMenuRepository.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(menu);

        var result = await service.DeleteAsync(1, 999, true);

        Assert.True(result);
        mockMenuRepository.Verify(r => r.SoftDeleteAsync(menu), Times.Once);
    }

    [Fact]
    public async Task RestoreAsync_NotDeleted_ReturnsFalse()
    {
        var (service, mockMenuRepository, _) = CreateService();
        var menu = new Menu { Id = 1, RestaurantId = 10, IsDeleted = false };
        mockMenuRepository.Setup(r => r.GetByIdIncludingDeletedAsync(1)).ReturnsAsync(menu);

        var result = await service.RestoreAsync(1, 5, false);

        Assert.False(result);
    }

    [Fact]
    public async Task RestoreAsync_DeletedAndOwner_ReturnsTrue()
    {
        var (service, mockMenuRepository, mockRestaurantRepository) = CreateService();
        var menu = new Menu { Id = 1, RestaurantId = 10, IsDeleted = true };
        mockMenuRepository.Setup(r => r.GetByIdIncludingDeletedAsync(1)).ReturnsAsync(menu);
        mockRestaurantRepository.Setup(r => r.GetByUserIdAsync(5)).ReturnsAsync([new Restaurant { Id = 10, UserId = 5 }]);

        var result = await service.RestoreAsync(1, 5, false);

        Assert.True(result);
        mockMenuRepository.Verify(r => r.RestoreAsync(menu), Times.Once);
    }

    [Fact]
    public async Task HardDeleteAsync_MenuNotFound_ReturnsFalse()
    {
        var (service, mockMenuRepository, _) = CreateService();
        mockMenuRepository.Setup(r => r.GetByIdIncludingDeletedAsync(99)).ReturnsAsync((Menu?)null);

        var result = await service.HardDeleteAsync(99, 1, false);

        Assert.False(result);
    }

    [Fact]
    public async Task HardDeleteAsync_Owner_CallsHardDelete()
    {
        var (service, mockMenuRepository, mockRestaurantRepository) = CreateService();
        var menu = new Menu { Id = 1, RestaurantId = 10 };
        mockMenuRepository.Setup(r => r.GetByIdIncludingDeletedAsync(1)).ReturnsAsync(menu);
        mockRestaurantRepository.Setup(r => r.GetByUserIdAsync(5)).ReturnsAsync([new Restaurant { Id = 10, UserId = 5 }]);

        var result = await service.HardDeleteAsync(1, 5, false);

        Assert.True(result);
        mockMenuRepository.Verify(r => r.HardDeleteAsync(menu), Times.Once);
    }

    [Fact]
    public async Task GetDeletedByRestaurantIdAsync_NotOwnerNotAdmin_ReturnsNull()
    {
        var (service, _, mockRestaurantRepository) = CreateService();
        mockRestaurantRepository.Setup(r => r.GetByUserIdAsync(5)).ReturnsAsync([]);

        var result = await service.GetDeletedByRestaurantIdAsync(10, 5, false);

        Assert.Null(result);
    }

    [Fact]
    public async Task GetDeletedByRestaurantIdAsync_Owner_ReturnsDeletedMenus()
    {
        var (service, mockMenuRepository, mockRestaurantRepository) = CreateService();
        mockRestaurantRepository.Setup(r => r.GetByUserIdAsync(5)).ReturnsAsync([new Restaurant { Id = 10, UserId = 5 }]);
        mockMenuRepository.Setup(r => r.GetDeletedByRestaurantIdAsync(10)).ReturnsAsync([new Menu { Id = 1, RestaurantId = 10, IsDeleted = true }]);

        var result = await service.GetDeletedByRestaurantIdAsync(10, 5, false);

        Assert.Single(result!);
    }
}
