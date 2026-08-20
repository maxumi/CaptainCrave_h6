using Api.Controllers;
using Api.DTOs;
using Api.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Moq;
using System.Security.Claims;

namespace Api.Tests.Controllers;

// Unit tests for MenusController.
// IMenuService and IRestaurantService are mocked so no database access occurs.
public class MenuControllerTests
{
    // Creates a MenusController with mocked services and an authenticated HttpContext (user id "1").
    private static (MenusController controller, Mock<IMenuService> mockService) CreateController()
    {
        var mockService = new Mock<IMenuService>();
        var mockRestaurantService = new Mock<IRestaurantService>();
        var controller = new MenusController(mockService.Object, mockRestaurantService.Object);
        var claims = new[] { new Claim(ClaimTypes.NameIdentifier, "1") };
        var user = new ClaimsPrincipal(new ClaimsIdentity(claims, "Test"));
        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = user }
        };
        return (controller, mockService);
    }

    // GetByRestaurant

    [Fact]
    public async Task GetByRestaurant_ReturnsOk()
    {
        var (controller, mockService) = CreateController();
        mockService.Setup(s => s.GetByRestaurantIdAsync(1)).ReturnsAsync([]);

        var result = await controller.GetByRestaurant(1);

        Assert.IsType<OkObjectResult>(result);
    }

    // GetById

    [Fact]
    public async Task GetById_ExistingId_ReturnsOk()
    {
        var (controller, mockService) = CreateController();
        var menu = new MenuDto { Id = 1, RestaurantId = 1, Name = "Lunch Menu" };
        mockService.Setup(s => s.GetByIdAsync(1)).ReturnsAsync(menu);

        var result = await controller.GetById(1);

        Assert.IsType<OkObjectResult>(result);
    }

    [Fact]
    public async Task GetById_NonExistingId_ReturnsNotFound()
    {
        var (controller, mockService) = CreateController();
        mockService.Setup(s => s.GetByIdAsync(99)).ReturnsAsync((MenuDto?)null);

        var result = await controller.GetById(99);

        Assert.IsType<NotFoundResult>(result);
    }

    // Create

    [Fact]
    public async Task Create_ValidDto_ReturnsCreatedAtAction()
    {
        var (controller, mockService) = CreateController();
        var dto = new CreateMenuDto { RestaurantId = 1, Name = "Lunch Menu" };
        var created = new MenuDto { Id = 5, RestaurantId = 1, Name = "Lunch Menu" };
        mockService.Setup(s => s.CreateAsync(dto)).ReturnsAsync(created);

        var result = await controller.Create(dto);

        Assert.IsType<CreatedAtActionResult>(result);
    }

    [Fact]
    public async Task Create_InvalidModelState_ReturnsBadRequest()
    {
        var (controller, _) = CreateController();
        controller.ModelState.AddModelError("Name", "Required");

        var result = await controller.Create(new CreateMenuDto());

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
        mockService.Setup(s => s.GetDeletedByRestaurantIdAsync(1, 1, false)).ReturnsAsync((IEnumerable<MenuDto>?)null);

        var result = await controller.GetDeleted(1) as StatusCodeResult;

        Assert.Equal(StatusCodes.Status403Forbidden, result?.StatusCode);
    }
}
