using Api.DTOs;
using Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Api.Controllers;

// Håndterer det falske/mock betalingssystem for ordrer.
// Controlleren håndtere requests fra klienten og returnerer det rigtige HTTP-svar.

[ApiController] // giver API opførsel til hele klassen.
[Route("api/[controller]")] 

public class PaymentsController(IPaymentService paymentService) : ControllerBase
{
    // DI: Gemmer dependency i en private readonly field (encapsu)
    private readonly IPaymentService _paymentService = paymentService;

    // POST: api/payments gennemfører en falsk betaling for en ordre der afventer betaling.
    [HttpPost] 
    [Authorize(Roles = "Customer, Admin")]
    public async Task<IActionResult> Create(CreatePaymentDto dto)
    {
        // checker om model-state er gyldig. 
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        // try-catch blokken håndterer forskellige fejlscenarier under betalingsprocessen.
        try
        {
            // Kalder service-laget for at gennemføre betalingen.
            var payment = await _paymentService.ProcessPaymentAsync(dto);
            return CreatedAtAction(nameof(GetByOrderId), new { orderId = payment.OrderId }, payment);
        }
        catch (KeyNotFoundException ex)
        {
            // Ordren findes ikke.
            return BadRequest(new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            // Ordren afventer ikke betaling, fx allerede betalt.
            return BadRequest(new { message = ex.Message });
        }
    }

    // GET: api/payments/order/{orderId} - henter det seneste betalingsforsøg for en given ordre.
    [HttpGet("order/{orderId}")]
    [Authorize]
    public async Task<IActionResult> GetByOrderId(int orderId)
    {
        var payment = await _paymentService.GetLatestByOrderIdAsync(orderId);
        if (payment is null)
            return NotFound(); // intet betalingsforsøg fundet for ordren endnu.

        return Ok(payment);
    }
}
