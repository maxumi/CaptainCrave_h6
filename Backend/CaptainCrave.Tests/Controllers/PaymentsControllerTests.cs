using Api.Controllers;
using Api.DTOs;
using Api.Models.Enums;
using Api.Services;
using Microsoft.AspNetCore.Mvc;
using Moq;

namespace Api.Tests.Controllers;

// UNIT-TEST 
// xUnit er testframeworket 

// Moq bruges til at mocke IPaymentService, så testen ikke bruger den rigtige database.
// Hver test følger AAA-mønsteret: Arrange (opsæt controller/mock/data), Act (kald metoden), Assert (tjek resultatet)
public class PaymentsControllerTests
{
 
    // Create: tester for forskellige scenarier ved oprettelse af betaling.

    [Fact] // testcase, kører kun en gang.
    public async Task Create_SuccessfulPayment_ReturnsCreatedAtAction()
    {
        // Arrange: opret controller/mock.

        // Opretter en ny controller til denne test, sammen med en mock af IPaymentService.
        // truple destructuring
        var (controller, mockService) = CreateController(); 

        // Bygger en CreatePaymentDto med standardværdier for denne test.
        var dto = new CreatePaymentDto { OrderId = 1, CardNumber = "4111111111111111" };

        // Opsætter mocken til at returnere en succesfuld betaling, når ProcessPaymentAsync kaldes med denne DTO.
        mockService.Setup(s => s.ProcessPaymentAsync(dto)).ReturnsAsync(MakePaymentDto());

        // Act: kald selve controller-metoden.
        var result = await controller.Create(dto);

        // Assert: tjek at svaret er en som forventet, 201 Created.
        Assert.IsType<CreatedAtActionResult>(result);
    }

    // Svaret indeholder den resulterende betalings-DTO.
    [Fact]
    public async Task Create_SuccessfulPayment_ReturnsPaymentDto()
    {
        // Arrange: samme opsætning som ovenfor, men gemmer DTO'en for at kunne sammenligne den senere.

        // Opretter en ny controller til denne test, sammen med en mock af IPaymentService.
        var (controller, mockService) = CreateController();

        // Bygger en CreatePaymentDto med standardværdier for denne test.
        var dto = new CreatePaymentDto { OrderId = 1, CardNumber = "4111111111111111" };

        // Bygger den forventede betalings-DTO, som vi bagefter kan sammenligne svaret med.
        var paymentDto = MakePaymentDto();

        // Opsætter mocken til at returnere netop denne DTO, når ProcessPaymentAsync kaldes.
        mockService.Setup(s => s.ProcessPaymentAsync(dto)).ReturnsAsync(paymentDto);

        // Act: kald controlleren, og cast resultatet så vi kan tilgå dens Value-egenskab.
        var result = await controller.Create(dto) as CreatedAtActionResult;

        // Assert: tjek at DTO'en i svaret er den samme, som servicen returnerede.
        Assert.Equal(paymentDto, result?.Value);
    }

    // Fejl i modelvalidering (fx manglende CardNumber) giver 400, før servicen kaldes.
    [Fact]
    public async Task Create_InvalidModelState_ReturnsBadRequest()
    {
        // Arrange: simulerer at [ApiController]'s modelvalidering allerede har fundet en fejl.

        // Opretter en ny controller til denne test, sammen med en mock af IPaymentService.
        var (controller, mockService) = CreateController();

        // Tilføjer manuelt en valideringsfejl, som normalt ville komme fra [ApiController]'s automatiske modelvalidering.
        controller.ModelState.AddModelError("CardNumber", "Required");

        // Bygger en DTO, der matcher fejlen (tomt kortnummer).
        var dto = new CreatePaymentDto { OrderId = 1, CardNumber = "" };

        // Act: kald controlleren med den ugyldige model.
        var result = await controller.Create(dto);

        // Assert: svaret skal være 400 Bad Request...
        Assert.IsType<BadRequestObjectResult>(result);
        // ...og servicen må aldrig være blevet kaldt, da valideringen fejlede først.
        mockService.Verify(s => s.ProcessPaymentAsync(It.IsAny<CreatePaymentDto>()), Times.Never);
    }

    // Et ukendt ordre-id bliver til 400, ikke 404, i tråd med servicens KeyNotFoundException-håndtering.
    [Fact]
    public async Task Create_UnknownOrder_ReturnsBadRequest()
    {
        // Arrange: mocken simulerer, at servicen ikke kunne finde ordren og kaster en exception.

        // Opretter en ny controller til denne test, sammen med en mock af IPaymentService.
        var (controller, mockService) = CreateController();

        // Bygger en DTO med et ordre-id, der ikke findes.
        var dto = new CreatePaymentDto { OrderId = 99, CardNumber = "4111111111111111" };

        // Opsætter mocken til at kaste den samme exception, som den rigtige service ville kaste her.
        mockService.Setup(s => s.ProcessPaymentAsync(dto)).ThrowsAsync(new KeyNotFoundException("Order 99 not found."));

        // Act: kald controlleren, som skal fange exception'en internt.
        var result = await controller.Create(dto);

        // Assert: bekræfter at exception'en blev oversat til 400, ikke en uhåndteret fejl.
        Assert.IsType<BadRequestObjectResult>(result);
    }

    // Betaling for en ordre, der ikke afventer betaling (allerede betalt, annulleret, ...) giver 400.
    [Fact]
    public async Task Create_OrderNotAwaitingPayment_ReturnsBadRequest()
    {
        // Arrange: mocken simulerer servicens tjek for, at ordren rent faktisk afventer betaling.

        // Opretter en ny controller til denne test, sammen med en mock af IPaymentService.
        var (controller, mockService) = CreateController();

        // Bygger en DTO for en ordre, der (iflølge mocken) ikke afventer betaling.
        var dto = new CreatePaymentDto { OrderId = 1, CardNumber = "4111111111111111" };

        // Opsætter mocken til at kaste den samme exception, som den rigtige service ville kaste her.
        mockService.Setup(s => s.ProcessPaymentAsync(dto)).ThrowsAsync(new InvalidOperationException("This order does not have a pending payment."));

        // Act: kald controlleren, som skal fange exception'en internt.
        var result = await controller.Create(dto);

        // Assert: bekræfter at exception'en blev oversat til 400.
        Assert.IsType<BadRequestObjectResult>(result);
    }

    // GetByOrderId

    // Giver 200 OK med det seneste betalingsforsøg, når et sådant findes.
    [Fact]
    public async Task GetByOrderId_PaymentExists_ReturnsOk()
    {
        // Arrange: mocken returnerer en eksisterende betaling for ordren.

        // Opretter en ny controller til denne test, sammen med en mock af IPaymentService.
        var (controller, mockService) = CreateController();

        // Opsætter mocken til at returnere en færdig betaling, når der spørges efter ordre-id 1.
        mockService.Setup(s => s.GetLatestByOrderIdAsync(1)).ReturnsAsync(MakePaymentDto());

        // Act: kald controlleren for at hente betalingen.
        var result = await controller.GetByOrderId(1);

        // Assert: svaret skal være 200 OK.
        Assert.IsType<OkObjectResult>(result);
    }

    // Giver 404 Not Found, når ordren endnu ikke har nogen betalingsforsøg.
    [Fact]
    public async Task GetByOrderId_NoPayment_ReturnsNotFound()
    {
        // Arrange: mocken simulerer, at der ikke findes nogen betaling for ordren (null).

        // Opretter en ny controller til denne test, sammen med en mock af IPaymentService.
        var (controller, mockService) = CreateController();

        // Opsætter mocken til at returnere null, som om ordren aldrig har haft et betalingsforsøg.
        mockService.Setup(s => s.GetLatestByOrderIdAsync(1)).ReturnsAsync((PaymentDto?)null);

        // Act: kald controlleren for at hente betalingen.
        var result = await controller.GetByOrderId(1);

        // Assert: svaret skal være 404 Not Found.
        Assert.IsType<NotFoundResult>(result);
    }

      // Hjælpemetode: bygger en frisk controller + mock til hver test, på den måde ikke deler tilstand.

    private static (PaymentsController controller, Mock<IPaymentService> mockService) CreateController()
    {
        // Opretter en mock af IPaymentService, som senere kan opsættes til at returnere bestemte svar.
        var mockService = new Mock<IPaymentService>();

        // Injects mock-objektet i controlleren i stedet for den rigtige service.
        var controller = new PaymentsController(mockService.Object);

        // Returnerer begge, så testen selv kan styre mocken og kalde controlleren.
        return (controller, mockService);
    }

    // Hjælpemetode: bygger en færdig PaymentDto med standardværdier, som kan overskrives per test.
    private static PaymentDto MakePaymentDto(int id = 1, int orderId = 1, PaymentStatus status = PaymentStatus.Succeeded) => new()
    {
        Id = id,
        OrderId = orderId,
        Amount = 99.50m,
        Status = status,
        ProviderReference = status == PaymentStatus.Succeeded ? "abc123" : null, // ProviderReference sætter transaction reference, når betalingen faktisk lykkedes (matcher rigtig service-logik).
        CreatedAt = new DateTime(2026, 6, 1)
    };
 
}
