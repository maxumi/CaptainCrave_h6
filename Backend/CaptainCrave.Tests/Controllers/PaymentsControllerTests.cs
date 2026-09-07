using Api.Controllers;
using Api.DTOs;
using Api.Models.Enums;
using Api.Services;
using Microsoft.AspNetCore.Mvc;
using Moq;

namespace Api.Tests.Controllers;

// Unit-tests for PaymentsController.
// IPaymentService bliver mocket, så der ikke køres database- eller gateway-logik.
// Controlleren læser aldrig selv ClaimsPrincipal (ejerskab håndhæves i service-laget
// via OrderRepository-opslåg), så der er ikke brug for en autentificeret bruger her.
public class PaymentsControllerTests
{
    /// <summary>Opretter betalingscontrolleren med en mocket service.</summary>
    /// <returns>Controlleren og dens mock, så testen kan styre servicesvaret.</returns>
    private static (PaymentsController controller, Mock<IPaymentService> mockService) CreateController()
    {
        var mockService = new Mock<IPaymentService>();
        var controller = new PaymentsController(mockService.Object);
        return (controller, mockService);
    }

    /// <summary>Bygger et betalingsresultat med faste testdata.</summary>
    /// <returns>En betalings-DTO, som controller-testene kan bruge.</returns>
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

    // En vellykket (falsk) betaling giver 201 Created, der peger på GetByOrderId.
    [Fact]
    public async Task Create_SuccessfulPayment_ReturnsCreatedAtAction()
    {
        var (controller, mockService) = CreateController();
        var dto = new CreatePaymentDto { OrderId = 1, CardNumber = "4111111111111111" };
        mockService.Setup(s => s.ProcessPaymentAsync(dto)).ReturnsAsync(MakePaymentDto());

        var result = await controller.Create(dto);

        Assert.IsType<CreatedAtActionResult>(result);
    }

    // Svaret indeholder den resulterende betalings-DTO.
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

    // Fejl i modelvalidering (fx manglende CardNumber) giver 400, før servicen kaldes.
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

    // Et ukendt ordre-id bliver til 400, ikke 404, i tråd med servicens KeyNotFoundException-håndtering.
    [Fact]
    public async Task Create_UnknownOrder_ReturnsBadRequest()
    {
        var (controller, mockService) = CreateController();
        var dto = new CreatePaymentDto { OrderId = 99, CardNumber = "4111111111111111" };
        mockService.Setup(s => s.ProcessPaymentAsync(dto)).ThrowsAsync(new KeyNotFoundException("Order 99 not found."));

        var result = await controller.Create(dto);

        Assert.IsType<BadRequestObjectResult>(result);
    }

    // Betaling for en ordre, der ikke afventer betaling (allerede betalt, annulleret, ...) giver 400.
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

    // Giver 200 OK med det seneste betalingsforsøg, når et sådant findes.
    [Fact]
    public async Task GetByOrderId_PaymentExists_ReturnsOk()
    {
        var (controller, mockService) = CreateController();
        mockService.Setup(s => s.GetLatestByOrderIdAsync(1)).ReturnsAsync(MakePaymentDto());

        var result = await controller.GetByOrderId(1);

        Assert.IsType<OkObjectResult>(result);
    }

    // Giver 404 Not Found, når ordren endnu ikke har nogen betalingsforsøg.
    [Fact]
    public async Task GetByOrderId_NoPayment_ReturnsNotFound()
    {
        var (controller, mockService) = CreateController();
        mockService.Setup(s => s.GetLatestByOrderIdAsync(1)).ReturnsAsync((PaymentDto?)null);

        var result = await controller.GetByOrderId(1);

        Assert.IsType<NotFoundResult>(result);
    }
}
