using Api.Controllers;
using Api.DTOs;
using Api.Models.Enums;
using Api.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using System.Security.Claims;

namespace Api.Tests.Controllers;

// Unit tests for OrdersController.
// IOrderService is mocked so no database, validation, or business logic runs.
// CreateController supplies a ClaimsPrincipal with Restaurant role so User.GetId()
// and User.GetRole() resolve without throwing inside the controller.
public class OrderControllerTests
{
    // Creates an OrdersController with a mocked IOrderService and a pre-authenticated user.
    private static (OrdersController controller, Mock<IOrderService> mockService) CreateController(
        int userId = 99, UserRole role = UserRole.Restaurant)
    {
        var mockService = new Mock<IOrderService>();
        var controller = new OrdersController(mockService.Object);
        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, userId.ToString()),
            new Claim(ClaimTypes.Role, role.ToString())
        };
        var user = new ClaimsPrincipal(new ClaimsIdentity(claims, "Test"));
        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = user }
        };
        return (controller, mockService);
    }

    private static OrderDto MakeOrderDto(int id = 1) => new()
    {
        Id = id,
        UserId = 10,
        UserName = "Alice",
        UserEmail = "alice@example.com",
        RestaurantId = 5,
        RestaurantName = "Burger Palace",
        Status = OrderStatus.Pending,
        DeliveryType = DeliveryType.Delivery,
        DeliveryAddress = "123 Main St",
        TotalPrice = 23.97m,
        CreatedAt = new DateTime(2026, 6, 1),
        UpdatedAt = new DateTime(2026, 6, 1),
        Items =
        [
            new OrderItemDto { Id = 1, MenuItemId = 3, MenuItemName = "Burger", Quantity = 2, Price = 9.99m },
            new OrderItemDto { Id = 2, MenuItemId = 7, MenuItemName = "Fries",  Quantity = 1, Price = 3.99m }
        ]
    };

    private static CreateOrderDto MakeCreateDto() => new()
    {
        UserId = 10,
        RestaurantId = 5,
        DeliveryType = DeliveryType.Delivery,
        DeliveryAddress = "123 Main St",
        Items =
        [
            new CreateOrderItemDto { MenuItemId = 3, Quantity = 2 },
            new CreateOrderItemDto { MenuItemId = 7, Quantity = 1 }
        ]
    };

    // GetById

    // Returns 200 OK when the order exists.
    [Fact]
    public async Task GetById_ExistingId_ReturnsOk()
    {
        var (controller, mockService) = CreateController();
        mockService.Setup(s => s.GetByIdAsync(1)).ReturnsAsync(MakeOrderDto());

        var result = await controller.GetById(1);

        Assert.IsType<OkObjectResult>(result);
    }

    // Response body contains the matching order DTO.
    [Fact]
    public async Task GetById_ExistingId_ReturnsOrderDto()
    {
        var (controller, mockService) = CreateController();
        var dto = MakeOrderDto();
        mockService.Setup(s => s.GetByIdAsync(1)).ReturnsAsync(dto);

        var result = await controller.GetById(1) as OkObjectResult;

        Assert.Equal(dto, result?.Value);
    }

    // Order DTO status field reflects the current order status.
    [Fact]
    public async Task GetById_ExistingId_ReturnsCorrectStatus()
    {
        var (controller, mockService) = CreateController();
        mockService.Setup(s => s.GetByIdAsync(1)).ReturnsAsync(MakeOrderDto());

        var result = await controller.GetById(1) as OkObjectResult;
        var order = result?.Value as OrderDto;

        Assert.Equal(OrderStatus.Pending, order?.Status);
    }

    // Returns 404 Not Found when no order matches the given ID.
    [Fact]
    public async Task GetById_NonExistingId_ReturnsNotFound()
    {
        var (controller, mockService) = CreateController();
        mockService.Setup(s => s.GetByIdAsync(99)).ReturnsAsync((OrderDto?)null);

        var result = await controller.GetById(99);

        Assert.IsType<NotFoundResult>(result);
    }

    // Create

    // Valid DTO returns 201 CreatedAtAction pointing to GetById.
    [Fact]
    public async Task Create_ValidDto_ReturnsCreatedAtAction()
    {
        var (controller, mockService) = CreateController();
        var dto = MakeCreateDto();
        mockService.Setup(s => s.CreateAsync(dto)).ReturnsAsync(MakeOrderDto());

        var result = await controller.Create(dto);

        Assert.IsType<CreatedAtActionResult>(result);
    }

    // Response body contains the newly created order.
    [Fact]
    public async Task Create_ValidDto_ReturnsCreatedOrder()
    {
        var (controller, mockService) = CreateController();
        var dto = MakeCreateDto();
        var created = MakeOrderDto();
        mockService.Setup(s => s.CreateAsync(dto)).ReturnsAsync(created);

        var result = await controller.Create(dto) as CreatedAtActionResult;

        Assert.Equal(created, result?.Value);
    }

    // CreatedAtAction route values reference the GetById action with the new order's ID.
    [Fact]
    public async Task Create_ValidDto_PointsToGetByIdRoute()
    {
        var (controller, mockService) = CreateController();
        var dto = MakeCreateDto();
        mockService.Setup(s => s.CreateAsync(dto)).ReturnsAsync(MakeOrderDto(id: 7));

        var result = await controller.Create(dto) as CreatedAtActionResult;

        Assert.Equal(nameof(controller.GetById), result?.ActionName);
        Assert.Equal(7, ((dynamic)result!.RouteValues!["id"]!));
    }

    // Invalid model state short-circuits before calling the service and returns 400 Bad Request.
    [Fact]
    public async Task Create_InvalidModelState_ReturnsBadRequest()
    {
        var (controller, _) = CreateController();
        controller.ModelState.AddModelError("Items", "Required");

        var result = await controller.Create(new CreateOrderDto());

        Assert.IsType<BadRequestObjectResult>(result);
    }

    // Service throws KeyNotFoundException for an unknown user, which the controller maps to 400 Bad Request.
    [Fact]
    public async Task Create_UnknownUser_ReturnsBadRequest()
    {
        var (controller, mockService) = CreateController();
        var dto = MakeCreateDto();
        mockService.Setup(s => s.CreateAsync(dto))
            .ThrowsAsync(new KeyNotFoundException("User 10 not found."));

        var result = await controller.Create(dto);

        Assert.IsType<BadRequestObjectResult>(result);
    }

    // Service throws KeyNotFoundException for an unknown restaurant, which maps to 400 Bad Request.
    [Fact]
    public async Task Create_UnknownRestaurant_ReturnsBadRequest()
    {
        var (controller, mockService) = CreateController();
        var dto = MakeCreateDto();
        mockService.Setup(s => s.CreateAsync(dto))
            .ThrowsAsync(new KeyNotFoundException("Restaurant 5 not found."));

        var result = await controller.Create(dto);

        Assert.IsType<BadRequestObjectResult>(result);
    }

    // Service throws KeyNotFoundException for an unknown menu item, which maps to 400 Bad Request.
    [Fact]
    public async Task Create_UnknownMenuItem_ReturnsBadRequest()
    {
        var (controller, mockService) = CreateController();
        var dto = MakeCreateDto();
        mockService.Setup(s => s.CreateAsync(dto))
            .ThrowsAsync(new KeyNotFoundException("Menu item 3 not found."));

        var result = await controller.Create(dto);

        Assert.IsType<BadRequestObjectResult>(result);
    }

    // UpdateStatus

    // Successful status update returns 200 OK with the updated order.
    [Fact]
    public async Task UpdateStatus_ExistingOrder_ReturnsOk()
    {
        var (controller, mockService) = CreateController();
        var dto = new UpdateOrderStatusDto { Status = OrderStatus.Preparing };
        mockService.Setup(s => s.UpdateStatusAsync(1, dto, It.IsAny<int>(), It.IsAny<UserRole>())).ReturnsAsync(true);
        mockService.Setup(s => s.GetByIdAsync(1)).ReturnsAsync(MakeOrderDto());

        var result = await controller.UpdateStatus(1, dto);

        Assert.IsType<OkObjectResult>(result);
    }

    // Returns 404 Not Found when no order matches the given ID.
    [Fact]
    public async Task UpdateStatus_NonExistingOrder_ReturnsNotFound()
    {
        var (controller, mockService) = CreateController();
        var dto = new UpdateOrderStatusDto { Status = OrderStatus.Preparing };
        mockService.Setup(s => s.UpdateStatusAsync(99, dto, It.IsAny<int>(), It.IsAny<UserRole>())).ReturnsAsync(false);

        var result = await controller.UpdateStatus(99, dto);

        Assert.IsType<NotFoundResult>(result);
    }

    // Invalid model state short-circuits before calling the service and returns 400 Bad Request.
    [Fact]
    public async Task UpdateStatus_InvalidModelState_ReturnsBadRequest()
    {
        var (controller, _) = CreateController();
        controller.ModelState.AddModelError("Status", "Required");

        var result = await controller.UpdateStatus(1, new UpdateOrderStatusDto());

        Assert.IsType<BadRequestObjectResult>(result);
    }

    // Every non-Pending status value results in 200 OK when the service confirms the update.
    [Theory]
    [InlineData(OrderStatus.Preparing)]
    [InlineData(OrderStatus.OnTheWay)]
    [InlineData(OrderStatus.ReadyForPickup)]
    [InlineData(OrderStatus.Delivered)]
    [InlineData(OrderStatus.Cancelled)]
    public async Task UpdateStatus_EachValidStatus_ReturnsOk(OrderStatus status)
    {
        var (controller, mockService) = CreateController();
        var dto = new UpdateOrderStatusDto { Status = status };
        mockService.Setup(s => s.UpdateStatusAsync(1, dto, It.IsAny<int>(), It.IsAny<UserRole>())).ReturnsAsync(true);
        mockService.Setup(s => s.GetByIdAsync(1)).ReturnsAsync(MakeOrderDto());

        var result = await controller.UpdateStatus(1, dto);

        Assert.IsType<OkObjectResult>(result);
    }
}

