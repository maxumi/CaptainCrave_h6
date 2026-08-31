using Api.Controllers;
using Api.DTOs;
using Api.Models.Enums;
using Api.Services;
using Microsoft.AspNetCore.Mvc;
using Moq;

namespace Api.Tests.Controllers;

// Unit tests for PaymentsController.
// IPaymentService is mocked so no database or gateway logic runs.
// The controller never reads ClaimsPrincipal itself (ownership is enforced in the service layer
// via OrderRepository lookups), so no authenticated user setup is needed here.
public class PaymentsControllerTests
{
    private static (PaymentsController controller, Mock<IPaymentService> mockService) CreateController()
    {
        var mockService = new Mock<IPaymentService>();
        var controller = new PaymentsController(mockService.Object);
        return (controller, mockService);
    }

    private static PaymentDto MakePaymentDto(int id = 1, int orderId = 1, PaymentStatus status = PaymentStatus.Succeeded) => new()
    {
        Id = id,
        OrderId = orderId,
        Amount = 99.50m,
        Status = status,
        ProviderReference = status == PaymentStatus.Succeeded ? "abc123" : null,
        CreatedAt = new DateTime(2026, 6, 1)
    };

    // Create

    // A successful (fake) charge returns 201 Created pointing at GetByOrderId.
    [Fact]
    public async Task Create_SuccessfulPayment_ReturnsCreatedAtAction()
    {
        var (controller, mockService) = CreateController();
        var dto = new CreatePaymentDto { OrderId = 1, CardNumber = "4111111111111111" };
        mockService.Setup(s => s.ProcessPaymentAsync(dto)).ReturnsAsync(MakePaymentDto());

        var result = await controller.Create(dto);

        Assert.IsType<CreatedAtActionResult>(result);
    }

    // Response body contains the resulting payment DTO.
    [Fact]
    public async Task Create_SuccessfulPayment_ReturnsPaymentDto()
    {
        var (controller, mockService) = CreateController();
        var dto = new CreatePaymentDto { OrderId = 1, CardNumber = "4111111111111111" };
        var paymentDto = MakePaymentDto();
        mockService.Setup(s => s.ProcessPaymentAsync(dto)).ReturnsAsync(paymentDto);

        var result = await controller.Create(dto) as CreatedAtActionResult;

        Assert.Equal(paymentDto, result?.Value);
    }

    // Model validation errors (e.g. missing CardNumber) return 400 before the service is called.
    [Fact]
    public async Task Create_InvalidModelState_ReturnsBadRequest()
    {
        var (controller, mockService) = CreateController();
        controller.ModelState.AddModelError("CardNumber", "Required");
        var dto = new CreatePaymentDto { OrderId = 1, CardNumber = "" };

        var result = await controller.Create(dto);

        Assert.IsType<BadRequestObjectResult>(result);
        mockService.Verify(s => s.ProcessPaymentAsync(It.IsAny<CreatePaymentDto>()), Times.Never);
    }

    // An unknown order ID becomes a 400, not a 404, matching the service's KeyNotFoundException mapping.
    [Fact]
    public async Task Create_UnknownOrder_ReturnsBadRequest()
    {
        var (controller, mockService) = CreateController();
        var dto = new CreatePaymentDto { OrderId = 99, CardNumber = "4111111111111111" };
        mockService.Setup(s => s.ProcessPaymentAsync(dto)).ThrowsAsync(new KeyNotFoundException("Order 99 not found."));

        var result = await controller.Create(dto);

        Assert.IsType<BadRequestObjectResult>(result);
    }

    // Paying for an order that isn't awaiting payment (already paid, cancelled, ...) returns 400.
    [Fact]
    public async Task Create_OrderNotAwaitingPayment_ReturnsBadRequest()
    {
        var (controller, mockService) = CreateController();
        var dto = new CreatePaymentDto { OrderId = 1, CardNumber = "4111111111111111" };
        mockService.Setup(s => s.ProcessPaymentAsync(dto)).ThrowsAsync(new InvalidOperationException("This order does not have a pending payment."));

        var result = await controller.Create(dto);

        Assert.IsType<BadRequestObjectResult>(result);
    }

    // GetByOrderId

    // Returns 200 OK with the latest payment attempt when one exists.
    [Fact]
    public async Task GetByOrderId_PaymentExists_ReturnsOk()
    {
        var (controller, mockService) = CreateController();
        mockService.Setup(s => s.GetLatestByOrderIdAsync(1)).ReturnsAsync(MakePaymentDto());

        var result = await controller.GetByOrderId(1);

        Assert.IsType<OkObjectResult>(result);
    }

    // Returns 404 Not Found when the order has no payment attempts yet.
    [Fact]
    public async Task GetByOrderId_NoPayment_ReturnsNotFound()
    {
        var (controller, mockService) = CreateController();
        mockService.Setup(s => s.GetLatestByOrderIdAsync(1)).ReturnsAsync((PaymentDto?)null);

        var result = await controller.GetByOrderId(1);

        Assert.IsType<NotFoundResult>(result);
    }
}
