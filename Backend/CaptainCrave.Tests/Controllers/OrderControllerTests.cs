using Api.Controllers;
using Api.DTOs;
using Api.Models.Enums;
using Api.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using System.Security.Claims;

namespace Api.Tests.Controllers;

// Unit-tests for OrdersController.
// IOrderService bliver mocket, så der ikke køres database, validering eller forretningslogik.
// CreateController giver en ClaimsPrincipal med Restaurant-rolle, så User.GetId()
// og User.GetRole() kan finde en værdi uden at kaste en fejl inde i controlleren.
public class OrderControllerTests
{
    /// <summary>
    /// Opretter en OrdersController med en mocket IOrderService og en allerede logget ind bruger.
    /// </summary>
    /// <returns>Controlleren og dens mock, så hver test kan bestemme servicesvaret.</returns>
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

    /// <summary>Bygger en gyldig ordre-DTO med faste testdata.</summary>
    /// <returns>En ordre, som controller-testene kan bruge som servicesvar.</returns>
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

    /// <summary>Bygger de standarddata, som en kunde sender for at oprette en ordre.</summary>
    /// <returns>En gyldig oprettelses-DTO med to retter.</returns>
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

    // Giver 200 OK når ordren findes.
    [Fact]
    public async Task GetById_ExistingId_ReturnsOk()
    {
        var (controller, mockService) = CreateController();
        mockService.Setup(s => s.GetByIdAsync(1)).ReturnsAsync(MakeOrderDto());

        var result = await controller.GetById(1);

        Assert.IsType<OkObjectResult>(result);
    }

    // Svaret indeholder den matchende ordre-DTO.
    [Fact]
    public async Task GetById_ExistingId_ReturnsOrderDto()
    {
        var (controller, mockService) = CreateController();
        var dto = MakeOrderDto();
        mockService.Setup(s => s.GetByIdAsync(1)).ReturnsAsync(dto);

        var result = await controller.GetById(1) as OkObjectResult;

        Assert.Equal(dto, result?.Value);
    }

    // Ordre-DTO'ens status-felt afspejler ordrens nuværende status.
    [Fact]
    public async Task GetById_ExistingId_ReturnsCorrectStatus()
    {
        var (controller, mockService) = CreateController();
        mockService.Setup(s => s.GetByIdAsync(1)).ReturnsAsync(MakeOrderDto());

        var result = await controller.GetById(1) as OkObjectResult;
        var order = result?.Value as OrderDto;

        Assert.Equal(OrderStatus.Pending, order?.Status);
    }

    // Giver 404 Not Found, når ingen ordre matcher det givne id.
    [Fact]
    public async Task GetById_NonExistingId_ReturnsNotFound()
    {
        var (controller, mockService) = CreateController();
        mockService.Setup(s => s.GetByIdAsync(99)).ReturnsAsync((OrderDto?)null);

        var result = await controller.GetById(99);

        Assert.IsType<NotFoundResult>(result);
    }

    // Create

    // Gyldig DTO giver 201 CreatedAtAction, der peger på GetById.
    [Fact]
    public async Task Create_ValidDto_ReturnsCreatedAtAction()
    {
        // Unit-test med AAA: Arrange gør klar, Act kalder Controlleren,
        // og Assert tjekker resultatet. Moq erstatter den rigtige Service.
        var (controller, mockService) = CreateController();
        var dto = MakeCreateDto();
        mockService.Setup(s => s.CreateAsync(dto)).ReturnsAsync(MakeOrderDto());

        var result = await controller.Create(dto);

        Assert.IsType<CreatedAtActionResult>(result);
    }

    // Svaret indeholder den nyoprettede ordre.
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

    // Route-værdierne i CreatedAtAction peger på GetById-handlingen med den nye ordres id.
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

    // Ugyldig model-state stopper forespørgslen før servicen kaldes og giver 400 Bad Request.
    [Fact]
    public async Task Create_InvalidModelState_ReturnsBadRequest()
    {
        var (controller, _) = CreateController();
        controller.ModelState.AddModelError("Items", "Required");

        var result = await controller.Create(new CreateOrderDto());

        Assert.IsType<BadRequestObjectResult>(result);
    }

    // Servicen kaster KeyNotFoundException for en ukendt bruger, som controlleren omdanner til 400 Bad Request.
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

    // Servicen kaster KeyNotFoundException for en ukendt restaurant, hvilket giver 400 Bad Request.
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

    // Servicen kaster KeyNotFoundException for en ukendt ret, hvilket giver 400 Bad Request.
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

    // En vellykket statusopdatering giver 200 OK med den opdaterede ordre.
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

    // Giver 404 Not Found, når ingen ordre matcher det givne id.
    [Fact]
    public async Task UpdateStatus_NonExistingOrder_ReturnsNotFound()
    {
        var (controller, mockService) = CreateController();
        var dto = new UpdateOrderStatusDto { Status = OrderStatus.Preparing };
        mockService.Setup(s => s.UpdateStatusAsync(99, dto, It.IsAny<int>(), It.IsAny<UserRole>())).ReturnsAsync(false);

        var result = await controller.UpdateStatus(99, dto);

        Assert.IsType<NotFoundResult>(result);
    }

    // Ugyldig model-state stopper forespørgslen før servicen kaldes og giver 400 Bad Request.
    [Fact]
    public async Task UpdateStatus_InvalidModelState_ReturnsBadRequest()
    {
        var (controller, _) = CreateController();
        controller.ModelState.AddModelError("Status", "Required");

        var result = await controller.UpdateStatus(1, new UpdateOrderStatusDto());

        Assert.IsType<BadRequestObjectResult>(result);
    }

    // Alle andre statusser end Pending giver 200 OK, når servicen bekræfter opdateringen.
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

    // Hvis brugeren har bestilt fra restauranten før, giver det 200 OK med true.
    [Fact]
    public async Task HasOrderedFromRestaurant_ServiceReturnsTrue_ReturnsOkWithTrue()
    {
        var (controller, mockService) = CreateController(userId: 7, role: UserRole.Customer);
        mockService.Setup(s => s.HasCustomerOrderedFromRestaurantAsync(7, 3)).ReturnsAsync(true);

        var result = await controller.HasOrderedFromRestaurant(3);

        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.Equal(true, okResult.Value?.GetType().GetProperty("hasOrdered")?.GetValue(okResult.Value));
    }

    // Hvis brugeren aldrig har bestilt fra restauranten, giver det 200 OK med false.
    [Fact]
    public async Task HasOrderedFromRestaurant_ServiceReturnsFalse_ReturnsOkWithFalse()
    {
        var (controller, mockService) = CreateController(userId: 7, role: UserRole.Customer);
        mockService.Setup(s => s.HasCustomerOrderedFromRestaurantAsync(7, 3)).ReturnsAsync(false);

        var result = await controller.HasOrderedFromRestaurant(3);

        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.Equal(false, okResult.Value?.GetType().GetProperty("hasOrdered")?.GetValue(okResult.Value));
    }
}

