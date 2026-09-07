using Api.DTOs;
using Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Api.Controllers;

// Håndterer det falske/mock betalingssystem for ordrer.
[ApiController]
[Route("api/[controller]")]
public class PaymentsController(IPaymentService paymentService) : ControllerBase
{
    private readonly IPaymentService _paymentService = paymentService;

    // POST: api/payments — gennemfører en (falsk) betaling for en ordre der afventer betaling.
    [HttpPost]
    [Authorize(Roles = "Customer,Admin")]
    public async Task<IActionResult> Create(CreatePaymentDto dto)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        try
        {
            var payment = await _paymentService.ProcessPaymentAsync(dto);
            return CreatedAtAction(nameof(GetByOrderId), new { orderId = payment.OrderId }, payment);
        }
        catch (KeyNotFoundException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    // GET: api/payments/order/{orderId} — henter det seneste betalingsforsøg for en given ordre.
    [HttpGet("order/{orderId}")]
    [Authorize]
    public async Task<IActionResult> GetByOrderId(int orderId)
    {
        var payment = await _paymentService.GetLatestByOrderIdAsync(orderId);
        if (payment is null)
            return NotFound();

        return Ok(payment);
    }
}
