using Api.Controllers;
using Api.DTOs;
using Api.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Moq;
using System.Security.Claims;

namespace Api.Tests.Controllers;

// Unit-tests for RestaurantsController.
// IRestaurantService og IMenuItemService bliver mocket, så der ikke sker nogen database-adgang.
public class RestaurantControllerTests
{
    // Opretter en RestaurantsController med mockede IRestaurantService og IMenuItemService.
    // Giver en autentificeret HttpContext, så User-claims kan findes inde i controlleren.
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

    // Opretter en RestaurantsController uden en autentificeret bruger, så GetCurrentUserId returnerer null.
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

    // Giver 200 OK for restaurant-listen.
    [Fact]
    public async Task GetAll_ReturnsOk()
    {
        var (controller, mockService, _, _) = CreateController();
        mockService.Setup(s => s.GetAllAsync()).ReturnsAsync([]);

        var result = await controller.GetAll();

        Assert.IsType<OkObjectResult>(result);
    }

    // Svaret indeholder hele restaurant-listen.
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

    // Giver 200 OK når restauranten findes.
    [Fact]
    public async Task GetById_ExistingId_ReturnsOk()
    {
        var (controller, mockService, _, _) = CreateController();
        var restaurant = new RestaurantDto { Id = 1, Name = "Burger Palace", UserId = 1 };
        mockService.Setup(s => s.GetByIdAsync(1)).ReturnsAsync(restaurant);

        var result = await controller.GetById(1);

        Assert.IsType<OkObjectResult>(result);
    }

    // Svaret indeholder den matchende restaurant-DTO.
    [Fact]
    public async Task GetById_ExistingId_ReturnsRestaurant()
    {
        var (controller, mockService, _, _) = CreateController();
        var restaurant = new RestaurantDto { Id = 1, Name = "Burger Palace", UserId = 1 };
        mockService.Setup(s => s.GetByIdAsync(1)).ReturnsAsync(restaurant);

        var result = await controller.GetById(1) as OkObjectResult;

        Assert.Equal(restaurant, result?.Value);
    }

    // Giver 404 Not Found, når ingen restaurant matcher det givne id.
    [Fact]
    public async Task GetById_NonExistingId_ReturnsNotFound()
    {
        var (controller, mockService, _, _) = CreateController();
        mockService.Setup(s => s.GetByIdAsync(99)).ReturnsAsync((RestaurantDto?)null);

        var result = await controller.GetById(99);

        Assert.IsType<NotFoundResult>(result);
    }

    // Create

    // Gyldig DTO giver 201 CreatedAtAction, der peger på GetById.
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

    // Svaret indeholder den nyoprettede restaurant.
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

    // Ugyldig model-state stopper forespørgslen før servicen kaldes og giver 400 Bad Request.
    [Fact]
    public async Task Create_InvalidModelState_ReturnsBadRequest()
    {
        var (controller, _, _, _) = CreateController();
        controller.ModelState.AddModelError("Name", "Required");

        var result = await controller.Create(new CreateRestaurantDto());

        Assert.IsType<BadRequestObjectResult>(result);
    }

    // GetMenuItems

    // Giver 200 OK for et gyldigt restaurant-id.
    [Fact]
    public async Task GetMenuItems_ReturnsOk()
    {
        var (controller, _, mockMenuItemService, _) = CreateController();
        mockMenuItemService.Setup(s => s.GetByRestaurantIdAsync(1)).ReturnsAsync([]);

        var result = await controller.GetMenuItems(1);

        Assert.IsType<OkObjectResult>(result);
    }

    // Svaret indeholder hele ret-listen for restauranten.
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

    // Giver 200 OK med en tom liste, når restauranten ingen retter har.
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

    // Ikke logget ind giver 401 Unauthorized.
    [Fact]
    public async Task UploadImage_NoUserIdClaim_ReturnsUnauthorized()
    {
        var controller = CreateUnauthenticatedController();

        var result = await controller.UploadImage(1, Mock.Of<IFormFile>());

        Assert.IsType<UnauthorizedResult>(result);
    }

    // Restaurant, der ikke findes, giver 404 Not Found.
    [Fact]
    public async Task UploadImage_RestaurantNotFound_ReturnsNotFound()
    {
        var (controller, mockService, _, _) = CreateController();
        mockService.Setup(s => s.GetByIdAsync(99)).ReturnsAsync((RestaurantDto?)null);

        var result = await controller.UploadImage(99, Mock.Of<IFormFile>());

        Assert.IsType<NotFoundResult>(result);
    }

    // Hvis brugeren ikke ejer restauranten, giver det 403 Forbidden.
    [Fact]
    public async Task UploadImage_NotOwner_ReturnsForbid()
    {
        var (controller, mockService, _, _) = CreateController();
        mockService.Setup(s => s.GetByIdAsync(1)).ReturnsAsync(new RestaurantDto { Id = 1, UserId = 999 });

        var result = await controller.UploadImage(1, Mock.Of<IFormFile>());

        Assert.IsType<ForbidResult>(result);
    }

    // En ugyldig filtype giver 400 Bad Request.
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

    // Gyldig fil gemmes, og svaret indeholder den opdaterede restaurant.
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

    // Hvis servicen fejler efter filen er gemt, bliver den nyligt uploadede fil slettet igen, og der gives 404.
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

    // Sletter en restaurant og giver 204 No Content.
    [Fact]
    public async Task Delete_ExistingId_ReturnsNoContent()
    {
        var (controller, mockService, _, _) = CreateController();
        mockService.Setup(s => s.DeleteAsync(1, 1, false)).ReturnsAsync(true);

        var result = await controller.Delete(1);

        Assert.IsType<NoContentResult>(result);
    }

    // Sletning af en restaurant, der ikke findes, giver 404 Not Found.
    [Fact]
    public async Task Delete_NonExistingId_ReturnsNotFound()
    {
        var (controller, mockService, _, _) = CreateController();
        mockService.Setup(s => s.DeleteAsync(99, 1, false)).ReturnsAsync(false);

        var result = await controller.Delete(99);

        Assert.IsType<NotFoundResult>(result);
    }

    // Restore

    // Gendanner en restaurant og giver 204 No Content.
    [Fact]
    public async Task Restore_ExistingId_ReturnsNoContent()
    {
        var (controller, mockService, _, _) = CreateController();
        mockService.Setup(s => s.RestoreAsync(1, 1, false)).ReturnsAsync(true);

        var result = await controller.Restore(1);

        Assert.IsType<NoContentResult>(result);
    }

    // Gendannelse af en restaurant, der ikke findes, giver 404 Not Found.
    [Fact]
    public async Task Restore_NonExistingId_ReturnsNotFound()
    {
        var (controller, mockService, _, _) = CreateController();
        mockService.Setup(s => s.RestoreAsync(99, 1, false)).ReturnsAsync(false);

        var result = await controller.Restore(99);

        Assert.IsType<NotFoundResult>(result);
    }

    // HardDelete (permanent sletning)

    // Sletter en restaurant for altid og giver 204 No Content.
    [Fact]
    public async Task HardDelete_ExistingId_ReturnsNoContent()
    {
        var (controller, mockService, _, _) = CreateController();
        mockService.Setup(s => s.HardDeleteAsync(1, 1, false)).ReturnsAsync(true);

        var result = await controller.HardDelete(1);

        Assert.IsType<NoContentResult>(result);
    }

    // En databasefejl under permanent sletning giver 409 Conflict.
    [Fact]
    public async Task HardDelete_WhenDbUpdateExceptionThrown_ReturnsConflict()
    {
        var (controller, mockService, _, _) = CreateController();
        mockService.Setup(s => s.HardDeleteAsync(1, 1, false)).ThrowsAsync(new DbUpdateException());

        var result = await controller.HardDelete(1);

        Assert.IsType<ConflictObjectResult>(result);
    }

    // GetDeleted (admin trash view)

    // Henter alle slettede restauranter og giver 200 OK.
    [Fact]
    public async Task GetDeleted_ReturnsOk()
    {
        var (controller, mockService, _, _) = CreateController();
        mockService.Setup(s => s.GetDeletedAsync()).ReturnsAsync([]);

        var result = await controller.GetDeleted();

        Assert.IsType<OkObjectResult>(result);
    }
}
