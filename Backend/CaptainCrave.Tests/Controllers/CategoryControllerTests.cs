using Api.Controllers;
using Api.DTOs;
using Api.Services;
using Microsoft.AspNetCore.Mvc;
using Moq;

namespace Api.Tests.Controllers;

// Unit tests for CategoriesController.
// ICategoryService is mocked so no database access occurs.
public class CategoryControllerTests
{
    // Creates a CategoriesController with a mocked ICategoryService.
    private static (CategoriesController controller, Mock<ICategoryService> mockService) CreateController()
    {
        var mockService = new Mock<ICategoryService>();
        var controller = new CategoriesController(mockService.Object);
        return (controller, mockService);
    }

    // GetByRestaurant

    // Returns 200 OK for a valid restaurant ID.
    [Fact]
    public async Task GetByRestaurant_ReturnsOk()
    {
        var (controller, mockService) = CreateController();
        mockService.Setup(s => s.GetByRestaurantIdAsync(1)).ReturnsAsync([]);

        var result = await controller.GetByRestaurant(1);

        Assert.IsType<OkObjectResult>(result);
    }

    // Response body contains the full category list.
    [Fact]
    public async Task GetByRestaurant_ReturnsCategories()
    {
        var (controller, mockService) = CreateController();
        var categories = new List<CategoryDto>
        {
            new() { Id = 1, RestaurantId = 1, Name = "Burgers" },
            new() { Id = 2, RestaurantId = 1, Name = "Drinks" }
        };
        mockService.Setup(s => s.GetByRestaurantIdAsync(1)).ReturnsAsync(categories);

        var result = await controller.GetByRestaurant(1) as OkObjectResult;

        Assert.Equal(categories, result?.Value);
    }

    // Returns 200 OK with an empty collection when the restaurant has no categories.
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

    // Valid DTO returns 201 Created.
    [Fact]
    public async Task Create_ValidDto_ReturnsCreated()
    {
        var (controller, mockService) = CreateController();
        var dto = new CreateCategoryDto { RestaurantId = 1, Name = "Burgers" };
        var created = new CategoryDto { Id = 3, RestaurantId = 1, Name = "Burgers" };
        mockService.Setup(s => s.CreateAsync(dto)).ReturnsAsync(created);

        var result = await controller.Create(dto);

        Assert.IsType<CreatedResult>(result);
    }

    // Response body contains the newly created category.
    [Fact]
    public async Task Create_ValidDto_ReturnsCreatedCategory()
    {
        var (controller, mockService) = CreateController();
        var dto = new CreateCategoryDto { RestaurantId = 1, Name = "Burgers" };
        var created = new CategoryDto { Id = 3, RestaurantId = 1, Name = "Burgers" };
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

        var result = await controller.Create(new CreateCategoryDto());

        Assert.IsType<BadRequestObjectResult>(result);
    }
}
