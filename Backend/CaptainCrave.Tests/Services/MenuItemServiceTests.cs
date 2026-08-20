using Api.DTOs;
using Api.Models;
using Api.Repositories;
using Api.Services;
using Moq;

namespace Api.Tests.Services;

// Unit tests for MenuItemService business logic. Ownership is resolved through the item's
// menu (via IMenuService), so IMenuService is mocked alongside the repositories.
public class MenuItemServiceTests
{
    private static (
        MenuItemService service,
        Mock<IMenuItemRepository> mockMenuItemRepository,
        Mock<IRestaurantRepository> mockRestaurantRepository,
        Mock<IMenuService> mockMenuService) CreateService()
    {
        var mockMenuItemRepository = new Mock<IMenuItemRepository>();
        var mockRestaurantRepository = new Mock<IRestaurantRepository>();
        var mockMenuService = new Mock<IMenuService>();
        var service = new MenuItemService(mockMenuItemRepository.Object, mockRestaurantRepository.Object, mockMenuService.Object);
        return (service, mockMenuItemRepository, mockRestaurantRepository, mockMenuService);
    }

    [Fact]
    public async Task CreateAsync_ValidDto_ReturnsCreatedMenuItem()
    {
        var (service, mockMenuItemRepository, _, _) = CreateService();
        mockMenuItemRepository.Setup(r => r.CreateAsync(It.IsAny<MenuItem>())).ReturnsAsync((MenuItem m) => { m.Id = 10; return m; });

        var result = await service.CreateAsync(new CreateMenuItemDto { MenuId = 1, Name = "Burger", Price = 50m });

        Assert.Equal(10, result.Id);
    }

    [Fact]
    public async Task UpdateAsync_ItemNotFound_ReturnsNull()
    {
        var (service, mockMenuItemRepository, _, _) = CreateService();
        mockMenuItemRepository.Setup(r => r.GetByIdAsync(99)).ReturnsAsync((MenuItem?)null);

        var result = await service.UpdateAsync(99, new CreateMenuItemDto { MenuId = 1, Name = "Burger", Price = 50m }, 1, false);

        Assert.Null(result);
    }

    [Fact]
    public async Task UpdateAsync_Owner_UpdatesFieldsAndReturnsDto()
    {
        var (service, mockMenuItemRepository, mockRestaurantRepository, mockMenuService) = CreateService();
        var existing = new MenuItem { Id = 1, MenuId = 20, Name = "Old Name", Price = 10m };
        mockMenuItemRepository.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(existing);
        mockMenuService.Setup(s => s.GetByIdIncludingDeletedAsync(20)).ReturnsAsync(new MenuDto { Id = 20, RestaurantId = 30 });
        mockRestaurantRepository.Setup(r => r.GetByUserIdAsync(5)).ReturnsAsync([new Restaurant { Id = 30, UserId = 5 }]);
        mockMenuItemRepository.Setup(r => r.UpdateAsync(existing)).ReturnsAsync(existing);

        var dto = new CreateMenuItemDto { MenuId = 20, Name = "New Name", Price = 99m, IsAvailable = false };
        var result = await service.UpdateAsync(1, dto, 5, false);

        Assert.Equal("New Name", result?.Name);
        Assert.Equal(99m, result?.Price);
        Assert.False(result?.IsAvailable);
    }

    [Fact]
    public async Task UpdateAsync_NotOwner_ReturnsNullAndDoesNotUpdate()
    {
        var (service, mockMenuItemRepository, mockRestaurantRepository, mockMenuService) = CreateService();
        var existing = new MenuItem { Id = 1, MenuId = 20, Name = "Old Name" };
        mockMenuItemRepository.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(existing);
        mockMenuService.Setup(s => s.GetByIdIncludingDeletedAsync(20)).ReturnsAsync(new MenuDto { Id = 20, RestaurantId = 30 });
        mockRestaurantRepository.Setup(r => r.GetByUserIdAsync(5)).ReturnsAsync([]);

        var result = await service.UpdateAsync(1, new CreateMenuItemDto { MenuId = 20, Name = "New Name" }, 5, false);

        Assert.Null(result);
        mockMenuItemRepository.Verify(r => r.UpdateAsync(It.IsAny<MenuItem>()), Times.Never);
    }

    [Fact]
    public async Task UpdateAsync_Admin_CanReassignMenuId()
    {
        var (service, mockMenuItemRepository, _, _) = CreateService();
        var existing = new MenuItem { Id = 1, MenuId = 20, Name = "Item" };
        mockMenuItemRepository.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(existing);
        mockMenuItemRepository.Setup(r => r.UpdateAsync(existing)).ReturnsAsync(existing);

        var dto = new CreateMenuItemDto { MenuId = 99, Name = "Item" };
        await service.UpdateAsync(1, dto, 1, true);

        Assert.Equal(99, existing.MenuId);
    }

    [Fact]
    public async Task DeleteAsync_Owner_ReturnsTrue()
    {
        var (service, mockMenuItemRepository, mockRestaurantRepository, mockMenuService) = CreateService();
        var existing = new MenuItem { Id = 1, MenuId = 20 };
        mockMenuItemRepository.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(existing);
        mockMenuService.Setup(s => s.GetByIdIncludingDeletedAsync(20)).ReturnsAsync(new MenuDto { Id = 20, RestaurantId = 30 });
        mockRestaurantRepository.Setup(r => r.GetByUserIdAsync(5)).ReturnsAsync([new Restaurant { Id = 30, UserId = 5 }]);

        var result = await service.DeleteAsync(1, 5, false);

        Assert.True(result);
        mockMenuItemRepository.Verify(r => r.SoftDeleteAsync(existing), Times.Once);
    }

    [Fact]
    public async Task RestoreAsync_NotDeleted_ReturnsFalse()
    {
        var (service, mockMenuItemRepository, _, _) = CreateService();
        mockMenuItemRepository.Setup(r => r.GetByIdIncludingDeletedAsync(1)).ReturnsAsync(new MenuItem { Id = 1, MenuId = 20, IsDeleted = false });

        var result = await service.RestoreAsync(1, 5, false);

        Assert.False(result);
    }

    [Fact]
    public async Task HardDeleteAsync_NotOwner_ReturnsFalse()
    {
        var (service, mockMenuItemRepository, mockRestaurantRepository, mockMenuService) = CreateService();
        var existing = new MenuItem { Id = 1, MenuId = 20 };
        mockMenuItemRepository.Setup(r => r.GetByIdIncludingDeletedAsync(1)).ReturnsAsync(existing);
        mockMenuService.Setup(s => s.GetByIdIncludingDeletedAsync(20)).ReturnsAsync(new MenuDto { Id = 20, RestaurantId = 30 });
        mockRestaurantRepository.Setup(r => r.GetByUserIdAsync(5)).ReturnsAsync([]);

        var result = await service.HardDeleteAsync(1, 5, false);

        Assert.False(result);
        mockMenuItemRepository.Verify(r => r.HardDeleteAsync(It.IsAny<MenuItem>()), Times.Never);
    }
}
