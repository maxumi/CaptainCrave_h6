using Api.DTOs;
using Api.Models;
using Api.Repositories;
using Api.Services;
using Moq;

namespace Api.Tests.Services;

// Unit-tests for MenuItemService's forretningslogik. Ejerskab afgøres via rettens
// menu (via IMenuService), så IMenuService bliver mocket sammen med repositories.
public class MenuItemServiceTests
{
    /// <summary>Opretter MenuItemService og alle dens database-, menu- og billedafhængigheder som mocks.</summary>
    /// <returns>Servicen og de fire mocks, som hver test kan sætte forventninger på.</returns>
    private static (
        MenuItemService service,
        Mock<IMenuItemRepository> mockMenuItemRepository,
        Mock<IRestaurantRepository> mockRestaurantRepository,
        Mock<IMenuService> mockMenuService,
        Mock<IImageStorageService> mockImageStorageService) CreateService()
    {
        var mockMenuItemRepository = new Mock<IMenuItemRepository>();
        var mockRestaurantRepository = new Mock<IRestaurantRepository>();
        var mockMenuService = new Mock<IMenuService>();
        var mockImageStorageService = new Mock<IImageStorageService>();
        var service = new MenuItemService(mockMenuItemRepository.Object, mockRestaurantRepository.Object, mockMenuService.Object, mockImageStorageService.Object);
        return (service, mockMenuItemRepository, mockRestaurantRepository, mockMenuService, mockImageStorageService);
    }

    // Opretter en gyldig ret, og den får et rigtigt id fra databasen.
    [Fact]
    public async Task CreateAsync_ValidDto_ReturnsCreatedMenuItem()
    {
        var (service, mockMenuItemRepository, _, _, _) = CreateService();
        mockMenuItemRepository.Setup(r => r.CreateAsync(It.IsAny<MenuItem>())).ReturnsAsync((MenuItem m) => { m.Id = 10; return m; });

        var result = await service.CreateAsync(new CreateMenuItemDto { MenuId = 1, Name = "Burger", Price = 50m });

        Assert.Equal(10, result.Id);
    }

    // Hvis retten slet ikke findes, kan man ikke opdatere den.
    [Fact]
    public async Task UpdateAsync_ItemNotFound_ReturnsNull()
    {
        var (service, mockMenuItemRepository, _, _, _) = CreateService();
        mockMenuItemRepository.Setup(r => r.GetByIdAsync(99)).ReturnsAsync((MenuItem?)null);

        var result = await service.UpdateAsync(99, new CreateMenuItemDto { MenuId = 1, Name = "Burger", Price = 50m }, 1, false);

        Assert.Null(result);
    }

    // Ejeren må opdatere navn, pris og tilgængelighed på sin ret.
    [Fact]
    public async Task UpdateAsync_Owner_UpdatesFieldsAndReturnsDto()
    {
        var (service, mockMenuItemRepository, mockRestaurantRepository, mockMenuService, _) = CreateService();
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

    // En bruger, der ikke ejer retten, må ikke opdatere den.
    [Fact]
    public async Task UpdateAsync_NotOwner_ReturnsNullAndDoesNotUpdate()
    {
        var (service, mockMenuItemRepository, mockRestaurantRepository, mockMenuService, _) = CreateService();
        var existing = new MenuItem { Id = 1, MenuId = 20, Name = "Old Name" };
        mockMenuItemRepository.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(existing);
        mockMenuService.Setup(s => s.GetByIdIncludingDeletedAsync(20)).ReturnsAsync(new MenuDto { Id = 20, RestaurantId = 30 });
        mockRestaurantRepository.Setup(r => r.GetByUserIdAsync(5)).ReturnsAsync([]);

        var result = await service.UpdateAsync(1, new CreateMenuItemDto { MenuId = 20, Name = "New Name" }, 5, false);

        Assert.Null(result);
        mockMenuItemRepository.Verify(r => r.UpdateAsync(It.IsAny<MenuItem>()), Times.Never);
    }

    // En admin må flytte retten til en anden menu uden at eje restauranten.
    [Fact]
    public async Task UpdateAsync_Admin_CanReassignMenuId()
    {
        var (service, mockMenuItemRepository, _, _, _) = CreateService();
        var existing = new MenuItem { Id = 1, MenuId = 20, Name = "Item" };
        mockMenuItemRepository.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(existing);
        mockMenuItemRepository.Setup(r => r.UpdateAsync(existing)).ReturnsAsync(existing);

        var dto = new CreateMenuItemDto { MenuId = 99, Name = "Item" };
        await service.UpdateAsync(1, dto, 1, true);

        Assert.Equal(99, existing.MenuId);
    }

    // Ejeren må slette sin ret, og den bliver soft-deleted.
    [Fact]
    public async Task DeleteAsync_Owner_ReturnsTrue()
    {
        var (service, mockMenuItemRepository, mockRestaurantRepository, mockMenuService, _) = CreateService();
        var existing = new MenuItem { Id = 1, MenuId = 20 };
        mockMenuItemRepository.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(existing);
        mockMenuService.Setup(s => s.GetByIdIncludingDeletedAsync(20)).ReturnsAsync(new MenuDto { Id = 20, RestaurantId = 30 });
        mockRestaurantRepository.Setup(r => r.GetByUserIdAsync(5)).ReturnsAsync([new Restaurant { Id = 30, UserId = 5 }]);

        var result = await service.DeleteAsync(1, 5, false);

        Assert.True(result);
        mockMenuItemRepository.Verify(r => r.SoftDeleteAsync(existing), Times.Once);
    }

    // Man kan ikke gendanne en ret, der slet ikke er slettet.
    [Fact]
    public async Task RestoreAsync_NotDeleted_ReturnsFalse()
    {
        var (service, mockMenuItemRepository, _, _, _) = CreateService();
        mockMenuItemRepository.Setup(r => r.GetByIdIncludingDeletedAsync(1)).ReturnsAsync(new MenuItem { Id = 1, MenuId = 20, IsDeleted = false });

        var result = await service.RestoreAsync(1, 5, false);

        Assert.False(result);
    }

    // En bruger, der ikke ejer restauranten, må ikke slette retten for altid.
    [Fact]
    public async Task HardDeleteAsync_NotOwner_ReturnsFalse()
    {
        var (service, mockMenuItemRepository, mockRestaurantRepository, mockMenuService, _) = CreateService();
        var existing = new MenuItem { Id = 1, MenuId = 20 };
        mockMenuItemRepository.Setup(r => r.GetByIdIncludingDeletedAsync(1)).ReturnsAsync(existing);
        mockMenuService.Setup(s => s.GetByIdIncludingDeletedAsync(20)).ReturnsAsync(new MenuDto { Id = 20, RestaurantId = 30 });
        mockRestaurantRepository.Setup(r => r.GetByUserIdAsync(5)).ReturnsAsync([]);

        var result = await service.HardDeleteAsync(1, 5, false);

        Assert.False(result);
        mockMenuItemRepository.Verify(r => r.HardDeleteAsync(It.IsAny<MenuItem>()), Times.Never);
    }

    // UpdateImageUrlAsync

    // Hvis retten ikke findes, sker der intet, og servicen sletter ikke nogen fil.
    [Fact]
    public async Task UpdateImageUrlAsync_ItemNotFound_ReturnsNull()
    {
        var (service, mockMenuItemRepository, _, _, mockImageStorageService) = CreateService();
        mockMenuItemRepository.Setup(r => r.GetByIdAsync(99)).ReturnsAsync((MenuItem?)null);

        var result = await service.UpdateImageUrlAsync(99, "/uploads/menu-items/new.jpg", 1, false);

        Assert.Null(result);
        mockImageStorageService.Verify(s => s.Delete(It.IsAny<string>()), Times.Never);
    }

    // En bruger, der ikke ejer retten, må ikke opdatere dens billede.
    [Fact]
    public async Task UpdateImageUrlAsync_NotOwner_ReturnsNullAndDoesNotUpdate()
    {
        var (service, mockMenuItemRepository, mockRestaurantRepository, mockMenuService, _) = CreateService();
        var existing = new MenuItem { Id = 1, MenuId = 20, ImageUrl = "/uploads/menu-items/old.jpg" };
        mockMenuItemRepository.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(existing);
        mockMenuService.Setup(s => s.GetByIdIncludingDeletedAsync(20)).ReturnsAsync(new MenuDto { Id = 20, RestaurantId = 30 });
        mockRestaurantRepository.Setup(r => r.GetByUserIdAsync(5)).ReturnsAsync([]);

        var result = await service.UpdateImageUrlAsync(1, "/uploads/menu-items/new.jpg", 5, false);

        Assert.Null(result);
        mockMenuItemRepository.Verify(r => r.UpdateAsync(It.IsAny<MenuItem>()), Times.Never);
    }

    // Ejeren må skifte billede, og det gamle billede bliver slettet fra disken.
    [Fact]
    public async Task UpdateImageUrlAsync_Owner_UpdatesImageAndDeletesPreviousFile()
    {
        var (service, mockMenuItemRepository, mockRestaurantRepository, mockMenuService, mockImageStorageService) = CreateService();
        var existing = new MenuItem { Id = 1, MenuId = 20, ImageUrl = "/uploads/menu-items/old.jpg" };
        mockMenuItemRepository.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(existing);
        mockMenuService.Setup(s => s.GetByIdIncludingDeletedAsync(20)).ReturnsAsync(new MenuDto { Id = 20, RestaurantId = 30 });
        mockRestaurantRepository.Setup(r => r.GetByUserIdAsync(5)).ReturnsAsync([new Restaurant { Id = 30, UserId = 5 }]);
        mockMenuItemRepository.Setup(r => r.UpdateAsync(existing)).ReturnsAsync(existing);

        var result = await service.UpdateImageUrlAsync(1, "/uploads/menu-items/new.jpg", 5, false);

        Assert.Equal("/uploads/menu-items/new.jpg", result?.ImageUrl);
        mockImageStorageService.Verify(s => s.Delete("/uploads/menu-items/old.jpg"), Times.Once);
    }

    // En admin må skifte billede uden selv at eje restauranten.
    [Fact]
    public async Task UpdateImageUrlAsync_Admin_BypassesOwnershipCheck()
    {
        var (service, mockMenuItemRepository, _, _, _) = CreateService();
        var existing = new MenuItem { Id = 1, MenuId = 20, ImageUrl = "/uploads/menu-items/old.jpg" };
        mockMenuItemRepository.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(existing);
        mockMenuItemRepository.Setup(r => r.UpdateAsync(existing)).ReturnsAsync(existing);

        var result = await service.UpdateImageUrlAsync(1, "/uploads/menu-items/new.jpg", 999, true);

        Assert.Equal("/uploads/menu-items/new.jpg", result?.ImageUrl);
    }
}
