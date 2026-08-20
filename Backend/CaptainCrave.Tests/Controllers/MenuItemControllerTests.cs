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
    private static (MenuItemsController controller, Mock<IMenuItemService> mockService) CreateController()
    {
        var mockService = new Mock<IMenuItemService>();
        var mockRestaurantService = new Mock<IRestaurantService>();
        var mockMenuService = new Mock<IMenuService>();
        var controller = new MenuItemsController(mockService.Object, mockRestaurantService.Object, mockMenuService.Object);
        var claims = new[] { new Claim(ClaimTypes.NameIdentifier, "1") };
        var user = new ClaimsPrincipal(new ClaimsIdentity(claims, "Test"));
        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = user }
        };
        return (controller, mockService);
    }

    // Create

    // Valid DTO returns 201 Created.
    [Fact]
    public async Task Create_ValidDto_ReturnsCreated()
    {
        var (controller, mockService) = CreateController();
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
        var (controller, mockService) = CreateController();
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
        var (controller, _) = CreateController();
        controller.ModelState.AddModelError("Name", "Required");

        var result = await controller.Create(new CreateMenuItemDto());

        Assert.IsType<BadRequestObjectResult>(result);
    }

    // Delete (soft delete)

    [Fact]
    public async Task Delete_ExistingId_ReturnsNoContent()
    {
        var (controller, mockService) = CreateController();
        mockService.Setup(s => s.DeleteAsync(1, 1, false)).ReturnsAsync(true);

        var result = await controller.Delete(1);

        Assert.IsType<NoContentResult>(result);
    }

    [Fact]
    public async Task Delete_NonExistingId_ReturnsNotFound()
    {
        var (controller, mockService) = CreateController();
        mockService.Setup(s => s.DeleteAsync(99, 1, false)).ReturnsAsync(false);

        var result = await controller.Delete(99);

        Assert.IsType<NotFoundResult>(result);
    }

    // Restore

    [Fact]
    public async Task Restore_ExistingId_ReturnsNoContent()
    {
        var (controller, mockService) = CreateController();
        mockService.Setup(s => s.RestoreAsync(1, 1, false)).ReturnsAsync(true);

        var result = await controller.Restore(1);

        Assert.IsType<NoContentResult>(result);
    }

    [Fact]
    public async Task Restore_NonExistingId_ReturnsNotFound()
    {
        var (controller, mockService) = CreateController();
        mockService.Setup(s => s.RestoreAsync(99, 1, false)).ReturnsAsync(false);

        var result = await controller.Restore(99);

        Assert.IsType<NotFoundResult>(result);
    }

    // HardDelete (permanent delete)

    [Fact]
    public async Task HardDelete_ExistingId_ReturnsNoContent()
    {
        var (controller, mockService) = CreateController();
        mockService.Setup(s => s.HardDeleteAsync(1, 1, false)).ReturnsAsync(true);

        var result = await controller.HardDelete(1);

        Assert.IsType<NoContentResult>(result);
    }

    [Fact]
    public async Task HardDelete_WhenDbUpdateExceptionThrown_ReturnsConflict()
    {
        var (controller, mockService) = CreateController();
        mockService.Setup(s => s.HardDeleteAsync(1, 1, false)).ThrowsAsync(new DbUpdateException());

        var result = await controller.HardDelete(1);

        Assert.IsType<ConflictObjectResult>(result);
    }

    // GetDeleted

    [Fact]
    public async Task GetDeleted_ReturnsOk()
    {
        var (controller, mockService) = CreateController();
        mockService.Setup(s => s.GetDeletedByRestaurantIdAsync(1, 1, false)).ReturnsAsync([]);

        var result = await controller.GetDeleted(1);

        Assert.IsType<OkObjectResult>(result);
    }

    [Fact]
    public async Task GetDeleted_NotOwner_ReturnsForbidden()
    {
        var (controller, mockService) = CreateController();
        mockService.Setup(s => s.GetDeletedByRestaurantIdAsync(1, 1, false)).ReturnsAsync((IEnumerable<MenuItemDto>?)null);

        var result = await controller.GetDeleted(1) as StatusCodeResult;

        Assert.Equal(StatusCodes.Status403Forbidden, result?.StatusCode);
    }
}
