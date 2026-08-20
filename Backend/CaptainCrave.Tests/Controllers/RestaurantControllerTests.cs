using Api.Controllers;
using Api.DTOs;
using Api.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Moq;
using System.Security.Claims;

namespace Api.Tests.Controllers;

// Unit tests for RestaurantsController.
// IRestaurantService and IMenuItemService are mocked so no database access occurs.
public class RestaurantControllerTests
{
    // Creates a RestaurantsController with mocked IRestaurantService and IMenuItemService.
    // Supplies an authenticated HttpContext so User claims resolve inside the controller.
    private static (RestaurantsController controller, Mock<IRestaurantService> mockService, Mock<IMenuItemService> mockMenuItemService, Mock<IImageStorageService> mockImageStorageService) CreateController()
    {
        var mockService = new Mock<IRestaurantService>();
        var mockMenuItemService = new Mock<IMenuItemService>();
        var mockMenuService = new Mock<IMenuService>();
        var mockImageStorageService = new Mock<IImageStorageService>();
        var controller = new RestaurantsController(mockService.Object, mockMenuItemService.Object, mockMenuService.Object, mockImageStorageService.Object);
        var claims = new[] { new Claim(ClaimTypes.NameIdentifier, "1") };
        var user = new ClaimsPrincipal(new ClaimsIdentity(claims, "Test"));
        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = user }
        };
        return (controller, mockService, mockMenuItemService, mockImageStorageService);
    }

    // Creates a RestaurantsController with no authenticated user, so GetCurrentUserId returns null.
    private static RestaurantsController CreateUnauthenticatedController()
    {
        var controller = new RestaurantsController(Mock.Of<IRestaurantService>(), Mock.Of<IMenuItemService>(), Mock.Of<IMenuService>(), Mock.Of<IImageStorageService>());
        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal(new ClaimsIdentity()) }
        };
        return controller;
    }

    // GetAll

    // Returns 200 OK for the restaurant listing.
    [Fact]
    public async Task GetAll_ReturnsOk()
    {
        var (controller, mockService, _, _) = CreateController();
        mockService.Setup(s => s.GetAllAsync()).ReturnsAsync([]);

        var result = await controller.GetAll();

        Assert.IsType<OkObjectResult>(result);
    }

    // Response body contains the full restaurant list.
    [Fact]
    public async Task GetAll_ReturnsRestaurantList()
    {
        var (controller, mockService, _, _) = CreateController();
        var restaurants = new List<RestaurantDto>
        {
            new() { Id = 1, Name = "Burger Palace", UserId = 1 },
            new() { Id = 2, Name = "Pizza Town", UserId = 2 }
        };
        mockService.Setup(s => s.GetAllAsync()).ReturnsAsync(restaurants);

        var result = await controller.GetAll() as OkObjectResult;

        Assert.Equal(restaurants, result?.Value);
    }

    // GetById

    // Returns 200 OK when the restaurant exists.
    [Fact]
    public async Task GetById_ExistingId_ReturnsOk()
    {
        var (controller, mockService, _, _) = CreateController();
        var restaurant = new RestaurantDto { Id = 1, Name = "Burger Palace", UserId = 1 };
        mockService.Setup(s => s.GetByIdAsync(1)).ReturnsAsync(restaurant);

        var result = await controller.GetById(1);

        Assert.IsType<OkObjectResult>(result);
    }

    // Response body contains the matching restaurant DTO.
    [Fact]
    public async Task GetById_ExistingId_ReturnsRestaurant()
    {
        var (controller, mockService, _, _) = CreateController();
        var restaurant = new RestaurantDto { Id = 1, Name = "Burger Palace", UserId = 1 };
        mockService.Setup(s => s.GetByIdAsync(1)).ReturnsAsync(restaurant);

        var result = await controller.GetById(1) as OkObjectResult;

        Assert.Equal(restaurant, result?.Value);
    }

    // Returns 404 Not Found when no restaurant matches the given ID.
    [Fact]
    public async Task GetById_NonExistingId_ReturnsNotFound()
    {
        var (controller, mockService, _, _) = CreateController();
        mockService.Setup(s => s.GetByIdAsync(99)).ReturnsAsync((RestaurantDto?)null);

        var result = await controller.GetById(99);

        Assert.IsType<NotFoundResult>(result);
    }

    // Create

    // Valid DTO returns 201 CreatedAtAction pointing to GetById.
    [Fact]
    public async Task Create_ValidDto_ReturnsCreatedAtAction()
    {
        var (controller, mockService, _, _) = CreateController();
        var dto = new CreateRestaurantDto { UserId = 1, Name = "New Place", Address = "123 Main St" };
        var created = new RestaurantDto { Id = 5, UserId = 1, Name = "New Place", Address = "123 Main St" };
        mockService.Setup(s => s.CreateAsync(dto)).ReturnsAsync(created);

        var result = await controller.Create(dto);

        Assert.IsType<CreatedAtActionResult>(result);
    }

    // Response body contains the newly created restaurant.
    [Fact]
    public async Task Create_ValidDto_ReturnsCreatedRestaurant()
    {
        var (controller, mockService, _, _) = CreateController();
        var dto = new CreateRestaurantDto { UserId = 1, Name = "New Place", Address = "123 Main St" };
        var created = new RestaurantDto { Id = 5, UserId = 1, Name = "New Place", Address = "123 Main St" };
        mockService.Setup(s => s.CreateAsync(dto)).ReturnsAsync(created);

        var result = await controller.Create(dto) as CreatedAtActionResult;

        Assert.Equal(created, result?.Value);
    }

    // Invalid model state short-circuits before calling the service and returns 400 Bad Request.
    [Fact]
    public async Task Create_InvalidModelState_ReturnsBadRequest()
    {
        var (controller, _, _, _) = CreateController();
        controller.ModelState.AddModelError("Name", "Required");

        var result = await controller.Create(new CreateRestaurantDto());

        Assert.IsType<BadRequestObjectResult>(result);
    }

    // GetMenuItems

    // Returns 200 OK for a valid restaurant ID.
    [Fact]
    public async Task GetMenuItems_ReturnsOk()
    {
        var (controller, _, mockMenuItemService, _) = CreateController();
        mockMenuItemService.Setup(s => s.GetByRestaurantIdAsync(1)).ReturnsAsync([]);

        var result = await controller.GetMenuItems(1);

        Assert.IsType<OkObjectResult>(result);
    }

    // Response body contains the full menu item list for the restaurant.
    [Fact]
    public async Task GetMenuItems_ReturnsItems()
    {
        var (controller, _, mockMenuItemService, _) = CreateController();
        var items = new List<MenuItemDto>
        {
            new() { Id = 1, MenuId = 1, Name = "Burger", Price = 9.99m },
            new() { Id = 2, MenuId = 1, Name = "Fries", Price = 3.49m }
        };
        mockMenuItemService.Setup(s => s.GetByRestaurantIdAsync(1)).ReturnsAsync(items);

        var result = await controller.GetMenuItems(1) as OkObjectResult;

        Assert.Equal(items, result?.Value);
    }

    // Returns 200 OK with an empty collection when the restaurant has no menu items.
    [Fact]
    public async Task GetMenuItems_EmptyList_ReturnsOkWithEmptyCollection()
    {
        var (controller, _, mockMenuItemService, _) = CreateController();
        mockMenuItemService.Setup(s => s.GetByRestaurantIdAsync(99)).ReturnsAsync([]);

        var result = await controller.GetMenuItems(99) as OkObjectResult;

        Assert.NotNull(result);
        Assert.Empty((IEnumerable<MenuItemDto>)result.Value!);
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
    public async Task UploadImage_RestaurantNotFound_ReturnsNotFound()
    {
        var (controller, mockService, _, _) = CreateController();
        mockService.Setup(s => s.GetByIdAsync(99)).ReturnsAsync((RestaurantDto?)null);

        var result = await controller.UploadImage(99, Mock.Of<IFormFile>());

        Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public async Task UploadImage_NotOwner_ReturnsForbid()
    {
        var (controller, mockService, _, _) = CreateController();
        mockService.Setup(s => s.GetByIdAsync(1)).ReturnsAsync(new RestaurantDto { Id = 1, UserId = 999 });

        var result = await controller.UploadImage(1, Mock.Of<IFormFile>());

        Assert.IsType<ForbidResult>(result);
    }

    [Fact]
    public async Task UploadImage_InvalidFile_ReturnsBadRequest()
    {
        var (controller, mockService, _, mockImageStorageService) = CreateController();
        mockService.Setup(s => s.GetByIdAsync(1)).ReturnsAsync(new RestaurantDto { Id = 1, UserId = 1 });
        mockImageStorageService
            .Setup(s => s.SaveAsync(It.IsAny<IFormFile>(), "restaurants"))
            .ThrowsAsync(new InvalidOperationException("Unsupported file type."));

        var result = await controller.UploadImage(1, Mock.Of<IFormFile>());

        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task UploadImage_ValidFile_ReturnsOkWithUpdatedRestaurant()
    {
        var (controller, mockService, _, mockImageStorageService) = CreateController();
        var restaurant = new RestaurantDto { Id = 1, UserId = 1, ImageUrl = "/uploads/restaurants/old.jpg" };
        var updated = new RestaurantDto { Id = 1, UserId = 1, ImageUrl = "/uploads/restaurants/new.jpg" };
        mockService.Setup(s => s.GetByIdAsync(1)).ReturnsAsync(restaurant);
        mockImageStorageService.Setup(s => s.SaveAsync(It.IsAny<IFormFile>(), "restaurants")).ReturnsAsync("/uploads/restaurants/new.jpg");
        mockService.Setup(s => s.UpdateImageUrlAsync(1, "/uploads/restaurants/new.jpg", 1, false)).ReturnsAsync(updated);

        var result = await controller.UploadImage(1, Mock.Of<IFormFile>()) as OkObjectResult;

        Assert.Equal(updated, result?.Value);
    }

    [Fact]
    public async Task UploadImage_ServiceReturnsNull_DeletesUploadedFileAndReturnsNotFound()
    {
        var (controller, mockService, _, mockImageStorageService) = CreateController();
        mockService.Setup(s => s.GetByIdAsync(1)).ReturnsAsync(new RestaurantDto { Id = 1, UserId = 1 });
        mockImageStorageService.Setup(s => s.SaveAsync(It.IsAny<IFormFile>(), "restaurants")).ReturnsAsync("/uploads/restaurants/new.jpg");
        mockService.Setup(s => s.UpdateImageUrlAsync(1, "/uploads/restaurants/new.jpg", 1, false)).ReturnsAsync((RestaurantDto?)null);

        var result = await controller.UploadImage(1, Mock.Of<IFormFile>());

        Assert.IsType<NotFoundResult>(result);
        mockImageStorageService.Verify(s => s.Delete("/uploads/restaurants/new.jpg"), Times.Once);
    }

    // Delete (soft delete)

    [Fact]
    public async Task Delete_ExistingId_ReturnsNoContent()
    {
        var (controller, mockService, _, _) = CreateController();
        mockService.Setup(s => s.DeleteAsync(1, 1, false)).ReturnsAsync(true);

        var result = await controller.Delete(1);

        Assert.IsType<NoContentResult>(result);
    }

    [Fact]
    public async Task Delete_NonExistingId_ReturnsNotFound()
    {
        var (controller, mockService, _, _) = CreateController();
        mockService.Setup(s => s.DeleteAsync(99, 1, false)).ReturnsAsync(false);

        var result = await controller.Delete(99);

        Assert.IsType<NotFoundResult>(result);
    }

    // Restore

    [Fact]
    public async Task Restore_ExistingId_ReturnsNoContent()
    {
        var (controller, mockService, _, _) = CreateController();
        mockService.Setup(s => s.RestoreAsync(1, 1, false)).ReturnsAsync(true);

        var result = await controller.Restore(1);

        Assert.IsType<NoContentResult>(result);
    }

    [Fact]
    public async Task Restore_NonExistingId_ReturnsNotFound()
    {
        var (controller, mockService, _, _) = CreateController();
        mockService.Setup(s => s.RestoreAsync(99, 1, false)).ReturnsAsync(false);

        var result = await controller.Restore(99);

        Assert.IsType<NotFoundResult>(result);
    }

    // HardDelete (permanent delete)

    [Fact]
    public async Task HardDelete_ExistingId_ReturnsNoContent()
    {
        var (controller, mockService, _, _) = CreateController();
        mockService.Setup(s => s.HardDeleteAsync(1, 1, false)).ReturnsAsync(true);

        var result = await controller.HardDelete(1);

        Assert.IsType<NoContentResult>(result);
    }

    [Fact]
    public async Task HardDelete_WhenDbUpdateExceptionThrown_ReturnsConflict()
    {
        var (controller, mockService, _, _) = CreateController();
        mockService.Setup(s => s.HardDeleteAsync(1, 1, false)).ThrowsAsync(new DbUpdateException());

        var result = await controller.HardDelete(1);

        Assert.IsType<ConflictObjectResult>(result);
    }

    // GetDeleted (admin trash view)

    [Fact]
    public async Task GetDeleted_ReturnsOk()
    {
        var (controller, mockService, _, _) = CreateController();
        mockService.Setup(s => s.GetDeletedAsync()).ReturnsAsync([]);

        var result = await controller.GetDeleted();

        Assert.IsType<OkObjectResult>(result);
    }
}
