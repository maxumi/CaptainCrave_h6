using Api.Controllers;
using Api.DTOs;
using Api.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Moq;
using System.Security.Claims;

namespace Api.Tests.Controllers;

// Unit tests for MenuItemsController.
// IMenuItemService and IRestaurantService are mocked so no database access occurs.
// An authenticated ClaimsPrincipal (user id "1") is supplied so GetCurrentUserId resolves inside the controller.
public class MenuItemControllerTests
{
    // Creates a MenuItemsController with mocked services and an authenticated HttpContext.
    private static (MenuItemsController controller, Mock<IMenuItemService> mockService, Mock<IImageStorageService> mockImageStorageService) CreateController()
    {
        var mockService = new Mock<IMenuItemService>();
        var mockRestaurantService = new Mock<IRestaurantService>();
        var mockMenuService = new Mock<IMenuService>();
        var mockImageStorageService = new Mock<IImageStorageService>();
        var controller = new MenuItemsController(mockService.Object, mockRestaurantService.Object, mockMenuService.Object, mockImageStorageService.Object);
        var claims = new[] { new Claim(ClaimTypes.NameIdentifier, "1") };
        var user = new ClaimsPrincipal(new ClaimsIdentity(claims, "Test"));
        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = user }
        };
        return (controller, mockService, mockImageStorageService);
    }

    // Creates a MenuItemsController with no authenticated user, so GetCurrentUserId returns null.
    private static MenuItemsController CreateUnauthenticatedController()
    {
        var controller = new MenuItemsController(Mock.Of<IMenuItemService>(), Mock.Of<IRestaurantService>(), Mock.Of<IMenuService>(), Mock.Of<IImageStorageService>());
        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal(new ClaimsIdentity()) }
        };
        return controller;
    }

    // Create

    // Valid DTO returns 201 Created.
    [Fact]
    public async Task Create_ValidDto_ReturnsCreated()
    {
        var (controller, mockService, _) = CreateController();
        var dto = new CreateMenuItemDto { MenuId = 1, CategoryId = 2, Name = "Burger", Price = 9.99m };
        var created = new MenuItemDto { Id = 10, MenuId = 1, CategoryId = 2, Name = "Burger", Price = 9.99m };
        mockService.Setup(s => s.CreateAsync(dto)).ReturnsAsync(created);

        var result = await controller.Create(dto);

        Assert.IsType<CreatedResult>(result);
    }

    // Response body contains the newly created menu item.
    [Fact]
    public async Task Create_ValidDto_ReturnsCreatedMenuItem()
    {
        var (controller, mockService, _) = CreateController();
        var dto = new CreateMenuItemDto { MenuId = 1, CategoryId = 2, Name = "Burger", Price = 9.99m };
        var created = new MenuItemDto { Id = 10, MenuId = 1, CategoryId = 2, Name = "Burger", Price = 9.99m };
        mockService.Setup(s => s.CreateAsync(dto)).ReturnsAsync(created);

        var result = await controller.Create(dto) as CreatedResult;

        Assert.Equal(created, result?.Value);
    }

    // Invalid model state short-circuits before calling the service and returns 400 Bad Request.
    [Fact]
    public async Task Create_InvalidModelState_ReturnsBadRequest()
    {
        var (controller, _, _) = CreateController();
        controller.ModelState.AddModelError("Name", "Required");

        var result = await controller.Create(new CreateMenuItemDto());

        Assert.IsType<BadRequestObjectResult>(result);
    }

    // UploadImage

    [Fact]
    public async Task UploadImage_NoUserIdClaim_ReturnsUnauthorized()
    {
        var controller = CreateUnauthenticatedController();

        var result = await controller.UploadImage(1, Mock.Of<IFormFile>());

        Assert.IsType<UnauthorizedResult>(result);
    }

    [Fact]
    public async Task UploadImage_InvalidFile_ReturnsBadRequest()
    {
        var (controller, _, mockImageStorageService) = CreateController();
        mockImageStorageService
            .Setup(s => s.SaveAsync(It.IsAny<IFormFile>(), "menu-items"))
            .ThrowsAsync(new InvalidOperationException("File is empty or exceeds the 5 MB limit."));

        var result = await controller.UploadImage(1, Mock.Of<IFormFile>());

        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task UploadImage_ValidFile_ReturnsOkWithUpdatedMenuItem()
    {
        var (controller, mockService, mockImageStorageService) = CreateController();
        var updated = new MenuItemDto { Id = 1, MenuId = 20, ImageUrl = "/uploads/menu-items/new.jpg" };
        mockImageStorageService.Setup(s => s.SaveAsync(It.IsAny<IFormFile>(), "menu-items")).ReturnsAsync("/uploads/menu-items/new.jpg");
        mockService.Setup(s => s.UpdateImageUrlAsync(1, "/uploads/menu-items/new.jpg", 1, false)).ReturnsAsync(updated);

        var result = await controller.UploadImage(1, Mock.Of<IFormFile>()) as OkObjectResult;

        Assert.Equal(updated, result?.Value);
    }

    [Fact]
    public async Task UploadImage_ServiceReturnsNull_DeletesUploadedFileAndReturnsNotFound()
    {
        var (controller, mockService, mockImageStorageService) = CreateController();
        mockImageStorageService.Setup(s => s.SaveAsync(It.IsAny<IFormFile>(), "menu-items")).ReturnsAsync("/uploads/menu-items/new.jpg");
        mockService.Setup(s => s.UpdateImageUrlAsync(1, "/uploads/menu-items/new.jpg", 1, false)).ReturnsAsync((MenuItemDto?)null);

        var result = await controller.UploadImage(1, Mock.Of<IFormFile>());

        Assert.IsType<NotFoundResult>(result);
        mockImageStorageService.Verify(s => s.Delete("/uploads/menu-items/new.jpg"), Times.Once);
    }

    // Delete (soft delete)

    [Fact]
    public async Task Delete_ExistingId_ReturnsNoContent()
    {
        var (controller, mockService, _) = CreateController();
        mockService.Setup(s => s.DeleteAsync(1, 1, false)).ReturnsAsync(true);

        var result = await controller.Delete(1);

        Assert.IsType<NoContentResult>(result);
    }

    [Fact]
    public async Task Delete_NonExistingId_ReturnsNotFound()
    {
        var (controller, mockService, _) = CreateController();
        mockService.Setup(s => s.DeleteAsync(99, 1, false)).ReturnsAsync(false);

        var result = await controller.Delete(99);

        Assert.IsType<NotFoundResult>(result);
    }

    // Restore

    [Fact]
    public async Task Restore_ExistingId_ReturnsNoContent()
    {
        var (controller, mockService, _) = CreateController();
        mockService.Setup(s => s.RestoreAsync(1, 1, false)).ReturnsAsync(true);

        var result = await controller.Restore(1);

        Assert.IsType<NoContentResult>(result);
    }

    [Fact]
    public async Task Restore_NonExistingId_ReturnsNotFound()
    {
        var (controller, mockService, _) = CreateController();
        mockService.Setup(s => s.RestoreAsync(99, 1, false)).ReturnsAsync(false);

        var result = await controller.Restore(99);

        Assert.IsType<NotFoundResult>(result);
    }

    // HardDelete (permanent delete)

    [Fact]
    public async Task HardDelete_ExistingId_ReturnsNoContent()
    {
        var (controller, mockService, _) = CreateController();
        mockService.Setup(s => s.HardDeleteAsync(1, 1, false)).ReturnsAsync(true);

        var result = await controller.HardDelete(1);

        Assert.IsType<NoContentResult>(result);
    }

    [Fact]
    public async Task HardDelete_WhenDbUpdateExceptionThrown_ReturnsConflict()
    {
        var (controller, mockService, _) = CreateController();
        mockService.Setup(s => s.HardDeleteAsync(1, 1, false)).ThrowsAsync(new DbUpdateException());

        var result = await controller.HardDelete(1);

        Assert.IsType<ConflictObjectResult>(result);
    }

    // GetDeleted

    [Fact]
    public async Task GetDeleted_ReturnsOk()
    {
        var (controller, mockService, _) = CreateController();
        mockService.Setup(s => s.GetDeletedByRestaurantIdAsync(1, 1, false)).ReturnsAsync([]);

        var result = await controller.GetDeleted(1);

        Assert.IsType<OkObjectResult>(result);
    }

    [Fact]
    public async Task GetDeleted_NotOwner_ReturnsForbidden()
    {
        var (controller, mockService, _) = CreateController();
        mockService.Setup(s => s.GetDeletedByRestaurantIdAsync(1, 1, false)).ReturnsAsync((IEnumerable<MenuItemDto>?)null);

        var result = await controller.GetDeleted(1) as StatusCodeResult;

        Assert.Equal(StatusCodes.Status403Forbidden, result?.StatusCode);
    }
}
