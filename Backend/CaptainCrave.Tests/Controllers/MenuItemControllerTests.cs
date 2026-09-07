using Api.Controllers;
using Api.DTOs;
using Api.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Moq;
using System.Security.Claims;

namespace Api.Tests.Controllers;

// Unit-tests for MenuItemsController.
// IMenuItemService og IRestaurantService bliver mocket, så der ikke sker nogen database-adgang.
// En autentificeret ClaimsPrincipal (bruger-id "1") gives, så GetCurrentUserId kan finde et id inde i controlleren.
public class MenuItemControllerTests
{
    // Opretter en MenuItemsController med mockede services og en autentificeret HttpContext.
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

    // Opretter en MenuItemsController uden en autentificeret bruger, så GetCurrentUserId returnerer null.
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

    // Gyldig DTO giver 201 Created.
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

    // Svaret indeholder den nyoprettede ret.
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

    // Ugyldig model-state stopper forespørgslen før servicen kaldes og giver 400 Bad Request.
    [Fact]
    public async Task Create_InvalidModelState_ReturnsBadRequest()
    {
        var (controller, _, _) = CreateController();
        controller.ModelState.AddModelError("Name", "Required");

        var result = await controller.Create(new CreateMenuItemDto());

        Assert.IsType<BadRequestObjectResult>(result);
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

    // En ugyldig fil (tom eller for stor) giver 400 Bad Request.
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

    // Gyldig fil gemmes, og svaret indeholder den opdaterede ret.
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

    // Hvis servicen fejler efter filen er gemt, bliver den nyligt uploadede fil slettet igen, og der gives 404.
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

    // Sletter en ret og giver 204 No Content.
    [Fact]
    public async Task Delete_ExistingId_ReturnsNoContent()
    {
        var (controller, mockService, _) = CreateController();
        mockService.Setup(s => s.DeleteAsync(1, 1, false)).ReturnsAsync(true);

        var result = await controller.Delete(1);

        Assert.IsType<NoContentResult>(result);
    }

    // Sletning af en ret, der ikke findes, giver 404 Not Found.
    [Fact]
    public async Task Delete_NonExistingId_ReturnsNotFound()
    {
        var (controller, mockService, _) = CreateController();
        mockService.Setup(s => s.DeleteAsync(99, 1, false)).ReturnsAsync(false);

        var result = await controller.Delete(99);

        Assert.IsType<NotFoundResult>(result);
    }

    // Restore

    // Gendanner en ret og giver 204 No Content.
    [Fact]
    public async Task Restore_ExistingId_ReturnsNoContent()
    {
        var (controller, mockService, _) = CreateController();
        mockService.Setup(s => s.RestoreAsync(1, 1, false)).ReturnsAsync(true);

        var result = await controller.Restore(1);

        Assert.IsType<NoContentResult>(result);
    }

    // Gendannelse af en ret, der ikke findes, giver 404 Not Found.
    [Fact]
    public async Task Restore_NonExistingId_ReturnsNotFound()
    {
        var (controller, mockService, _) = CreateController();
        mockService.Setup(s => s.RestoreAsync(99, 1, false)).ReturnsAsync(false);

        var result = await controller.Restore(99);

        Assert.IsType<NotFoundResult>(result);
    }

    // HardDelete (permanent sletning)

    // Sletter en ret for altid og giver 204 No Content.
    [Fact]
    public async Task HardDelete_ExistingId_ReturnsNoContent()
    {
        var (controller, mockService, _) = CreateController();
        mockService.Setup(s => s.HardDeleteAsync(1, 1, false)).ReturnsAsync(true);

        var result = await controller.HardDelete(1);

        Assert.IsType<NoContentResult>(result);
    }

    // En databasefejl under permanent sletning giver 409 Conflict.
    [Fact]
    public async Task HardDelete_WhenDbUpdateExceptionThrown_ReturnsConflict()
    {
        var (controller, mockService, _) = CreateController();
        mockService.Setup(s => s.HardDeleteAsync(1, 1, false)).ThrowsAsync(new DbUpdateException());

        var result = await controller.HardDelete(1);

        Assert.IsType<ConflictObjectResult>(result);
    }

    // GetDeleted

    // Henter slettede retter for restaurantens ejer og giver 200 OK.
    [Fact]
    public async Task GetDeleted_ReturnsOk()
    {
        var (controller, mockService, _) = CreateController();
        mockService.Setup(s => s.GetDeletedByRestaurantIdAsync(1, 1, false)).ReturnsAsync([]);

        var result = await controller.GetDeleted(1);

        Assert.IsType<OkObjectResult>(result);
    }

    // Hvis brugeren ikke ejer restauranten, giver det 403 Forbidden.
    [Fact]
    public async Task GetDeleted_NotOwner_ReturnsForbidden()
    {
        var (controller, mockService, _) = CreateController();
        mockService.Setup(s => s.GetDeletedByRestaurantIdAsync(1, 1, false)).ReturnsAsync((IEnumerable<MenuItemDto>?)null);

        var result = await controller.GetDeleted(1) as StatusCodeResult;

        Assert.Equal(StatusCodes.Status403Forbidden, result?.StatusCode);
    }
}
