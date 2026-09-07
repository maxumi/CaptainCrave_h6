using Api.Controllers;
using Api.DTOs;
using Api.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Moq;
using System.Security.Claims;

namespace Api.Tests.Controllers;

// Unit-tests for CategoriesController.
// ICategoryService bliver mocket, så der ikke sker nogen database-adgang.
public class CategoryControllerTests
{
    // Opretter en CategoriesController med en mocket ICategoryService og en autentificeret HttpContext (bruger-id "1").
    private static (CategoriesController controller, Mock<ICategoryService> mockService) CreateController()
    {
        var mockService = new Mock<ICategoryService>();
        var controller = new CategoriesController(mockService.Object);
        var claims = new[] { new Claim(ClaimTypes.NameIdentifier, "1") };
        var user = new ClaimsPrincipal(new ClaimsIdentity(claims, "Test"));
        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = user }
        };
        return (controller, mockService);
    }

    // GetByRestaurant

    // Giver 200 OK for et gyldigt restaurant-id.
    [Fact]
    public async Task GetByRestaurant_ReturnsOk()
    {
        var (controller, mockService) = CreateController();
        mockService.Setup(s => s.GetByRestaurantIdAsync(1)).ReturnsAsync([]);

        var result = await controller.GetByRestaurant(1);

        Assert.IsType<OkObjectResult>(result);
    }

    // Svaret indeholder hele kategori-listen.
    [Fact]
    public async Task GetByRestaurant_ReturnsCategories()
    {
        var (controller, mockService) = CreateController();
        var categories = new List<CategoryDto>
        {
            new() { Id = 1, MenuId = 1, Name = "Burgers" },
            new() { Id = 2, MenuId = 1, Name = "Drinks" }
        };
        mockService.Setup(s => s.GetByRestaurantIdAsync(1)).ReturnsAsync(categories);

        var result = await controller.GetByRestaurant(1) as OkObjectResult;

        Assert.Equal(categories, result?.Value);
    }

    // Giver 200 OK med en tom liste, når restauranten ingen kategorier har.
    [Fact]
    public async Task GetByRestaurant_EmptyList_ReturnsOkWithEmptyCollection()
    {
        var (controller, mockService) = CreateController();
        mockService.Setup(s => s.GetByRestaurantIdAsync(99)).ReturnsAsync([]);

        var result = await controller.GetByRestaurant(99) as OkObjectResult;

        Assert.NotNull(result);
        Assert.Empty((IEnumerable<CategoryDto>)result.Value!);
    }

    // Create

    // Gyldig DTO giver 201 Created.
    [Fact]
    public async Task Create_ValidDto_ReturnsCreated()
    {
        var (controller, mockService) = CreateController();
        var dto = new CreateCategoryDto { MenuId = 1, Name = "Burgers" };
        var created = new CategoryDto { Id = 3, MenuId = 1, Name = "Burgers" };
        mockService.Setup(s => s.CreateAsync(dto)).ReturnsAsync(created);

        var result = await controller.Create(dto);

        Assert.IsType<CreatedResult>(result);
    }

    // Svaret indeholder den nyoprettede kategori.
    [Fact]
    public async Task Create_ValidDto_ReturnsCreatedCategory()
    {
        var (controller, mockService) = CreateController();
        var dto = new CreateCategoryDto { MenuId = 1, Name = "Burgers" };
        var created = new CategoryDto { Id = 3, MenuId = 1, Name = "Burgers" };
        mockService.Setup(s => s.CreateAsync(dto)).ReturnsAsync(created);

        var result = await controller.Create(dto) as CreatedResult;

        Assert.Equal(created, result?.Value);
    }

    // Ugyldig model-state stopper forespørgslen før servicen kaldes og giver 400 Bad Request.
    [Fact]
    public async Task Create_InvalidModelState_ReturnsBadRequest()
    {
        var (controller, _) = CreateController();
        controller.ModelState.AddModelError("Name", "Required");

        var result = await controller.Create(new CreateCategoryDto());

        Assert.IsType<BadRequestObjectResult>(result);
    }

    // Delete (soft delete)

    // Sletter en kategori og giver 204 No Content.
    [Fact]
    public async Task Delete_ExistingId_ReturnsNoContent()
    {
        var (controller, mockService) = CreateController();
        mockService.Setup(s => s.DeleteAsync(1, 1, false)).ReturnsAsync(true);

        var result = await controller.Delete(1);

        Assert.IsType<NoContentResult>(result);
    }

    // Sletning af en kategori, der ikke findes, giver 404 Not Found.
    [Fact]
    public async Task Delete_NonExistingId_ReturnsNotFound()
    {
        var (controller, mockService) = CreateController();
        mockService.Setup(s => s.DeleteAsync(99, 1, false)).ReturnsAsync(false);

        var result = await controller.Delete(99);

        Assert.IsType<NotFoundResult>(result);
    }

    // Restore

    // En slettet kategori kan gendannes, og controlleren svarer med 204 No Content.
    [Fact]
    public async Task Restore_ExistingId_ReturnsNoContent()
    {
        var (controller, mockService) = CreateController();
        mockService.Setup(s => s.RestoreAsync(1, 1, false)).ReturnsAsync(true);

        var result = await controller.Restore(1);

        Assert.IsType<NoContentResult>(result);
    }

    // Gendannelse af en kategori, der ikke findes, giver 404 Not Found.
    [Fact]
    public async Task Restore_NonExistingId_ReturnsNotFound()
    {
        var (controller, mockService) = CreateController();
        mockService.Setup(s => s.RestoreAsync(99, 1, false)).ReturnsAsync(false);

        var result = await controller.Restore(99);

        Assert.IsType<NotFoundResult>(result);
    }

    // HardDelete (permanent sletning)

    // Sletter en kategori for altid og giver 204 No Content.
    [Fact]
    public async Task HardDelete_ExistingId_ReturnsNoContent()
    {
        var (controller, mockService) = CreateController();
        mockService.Setup(s => s.HardDeleteAsync(1, 1, false)).ReturnsAsync(true);

        var result = await controller.HardDelete(1);

        Assert.IsType<NoContentResult>(result);
    }

    // En databasefejl under permanent sletning giver 409 Conflict.
    [Fact]
    public async Task HardDelete_WhenDbUpdateExceptionThrown_ReturnsConflict()
    {
        var (controller, mockService) = CreateController();
        mockService.Setup(s => s.HardDeleteAsync(1, 1, false)).ThrowsAsync(new DbUpdateException());

        var result = await controller.HardDelete(1);

        Assert.IsType<ConflictObjectResult>(result);
    }

    // GetDeleted

    // Henter slettede kategorier for restaurantens ejer og giver 200 OK.
    [Fact]
    public async Task GetDeleted_ReturnsOk()
    {
        var (controller, mockService) = CreateController();
        mockService.Setup(s => s.GetDeletedByRestaurantIdAsync(1, 1, false)).ReturnsAsync([]);

        var result = await controller.GetDeleted(1);

        Assert.IsType<OkObjectResult>(result);
    }

    // Hvis brugeren ikke ejer restauranten, giver det 403 Forbidden.
    [Fact]
    public async Task GetDeleted_NotOwner_ReturnsForbidden()
    {
        var (controller, mockService) = CreateController();
        mockService.Setup(s => s.GetDeletedByRestaurantIdAsync(1, 1, false)).ReturnsAsync((IEnumerable<CategoryDto>?)null);

        var result = await controller.GetDeleted(1) as StatusCodeResult;

        Assert.Equal(StatusCodes.Status403Forbidden, result?.StatusCode);
    }
}
