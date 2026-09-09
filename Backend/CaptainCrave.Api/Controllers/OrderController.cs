using Api.DTOs;
using Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Api.Controllers;

// Håndterer requests relateret til kunde- og restaurantordrer.
[ApiController]
[Route("api/[controller]")]

// primary constructor 
public class OrdersController(IOrderService orderService) : ControllerBase
{
    // DI for OrderService.
    private readonly IOrderService _orderService = orderService;

    // Controlleren håndterer HTTP: input valideres, og arbejdet sendes til OrderService.
    // POST: api/orders — Opretter en ny ordre for en kunde eller administrator.
    [HttpPost]
    [Authorize(Roles = "Customer,Admin")] // Kun kunder og administratorer kan oprette ordrer.
    public async Task<IActionResult> Create(CreateOrderDto dto)
    {
        // Kontrollerer om request-data opfylder valideringskravene. 
        // Hvis ikke, returneres en 400 Bad Request med fejlbeskeder.
        if (!ModelState.IsValid)
            return BadRequest(ModelState);
        
        try
        {
            var created = await _orderService.CreateAsync(dto);
            return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
        }
        catch (KeyNotFoundException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    // GET: api/orders/{id} — Henter en ordre ud fra dens ID.
    [HttpGet("{id}")]
    [Authorize]
    public async Task<IActionResult> GetById(int id)
    {
        var order = await _orderService.GetByIdAsync(id);
        if (order is null)
            return NotFound();

        return Ok(order);
    }

    // GET: api/orders/active — Henter den aktive ordre for den aktuelle kunde.
    [HttpGet("customer/active")]
    [Authorize(Roles = "Customer")]
    public async Task<IActionResult> GetActiveOrderForCustomer()
    {
        // Henter den autentificerede brugers ID fra JWT-tokenet.
        var userId = User.GetId();

        // Henter kundens aktive ordre. 
        // Hvis der ikke findes nogen aktiv ordre, returneres 404 Not Found.
        var order = await _orderService.GetActiveOrderForUserAsync(userId);

        if (order is null)
            return NotFound();

        return Ok(order);
    }

    // Bevarer den tidligere route, så eksisterende klientkode fortsat virker.
    [HttpGet("active")]
    [Authorize(Roles = "Customer")]
    public async Task<IActionResult> GetActiveOrder() => await GetActiveOrderForCustomer();

    // GET: api/orders/customer/has-ordered/{restaurantId} — Kontrollerer om den aktuelle kunde 
    // tidligere har fået leveret en ordre fra restauranten.
    // Resultatet bruges blandt andet til at afgøre, om kunden må anmelde restauranten.
    [HttpGet("customer/has-ordered/{restaurantId:int}")]
    [Authorize(Roles = "Customer")]
    public async Task<IActionResult> HasOrderedFromRestaurant(int restaurantId)
    {
        var userId = User.GetId();
        var hasOrdered = await _orderService.HasCustomerOrderedFromRestaurantAsync(userId, restaurantId);
        return Ok(new { hasOrdered });
    }

    // Henter kundens afsluttede og historiske ordrer.
    [HttpGet("customer/history")]
    [Authorize(Roles = "Customer")]
    public async Task<IActionResult> GetCustomerHistory()
    {
        // Henter den autentificerede brugers ID fra JWT-tokenet.
        var userId = User.GetId();

        // Returnerer en tom liste, hvis kunden ikke har nogen historiske ordrer.
        var orders = await _orderService.GetHistoricOrdersForUserAsync(userId);
        return Ok(orders);
    }

    // Henter alle aktive ordrer for den restaurant, som brugeren har adgang til.
    [HttpGet("restaurant/active")]
    [Authorize(Roles = "Restaurant,Admin")]
    public async Task<IActionResult> GetRestaurantActiveOrders()
    {
        var userId = User.GetId();
        var role = User.GetRole();
        try
        {
            // Henter aktive ordrer for restauranten, som brugeren har adgang til.
            var orders = await _orderService.GetRestaurantActiveOrdersAsync(userId, role);
            return Ok(orders);
        }
        catch (UnauthorizedAccessException ex)
        {
            // Brugeren har ikke adgang til restaurantens ordrer.
            return StatusCode(StatusCodes.Status403Forbidden, new { message = ex.Message });
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

    // Henter alle afsluttede ordrer for en restaurant.
    [HttpGet("restaurant/history")]
    [Authorize(Roles = "Restaurant,Admin")]
    public async Task<IActionResult> GetRestaurantHistoricOrders()
    {
        var userId = User.GetId();
        var role = User.GetRole();
        try
        {
            // Henter afsluttede restaurant ordrer.
            var orders = await _orderService.GetRestaurantHistoricOrdersAsync(userId, role);
            return Ok(orders);
        }
        catch (UnauthorizedAccessException ex)
        {
            return StatusCode(StatusCodes.Status403Forbidden, new { message = ex.Message });
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

    // PATCH: api/orders/{id}/status")]
    // Opdaterer status på en ordre.
    // Kun restaurantbrugere og administratorer kan opdatere status på en ordre, undtagen for kunder, 
    // som kun kan opdatere til "Cancelled".
    // Adgang og gyldige statusskift kontrolleres i OrderService.
    [HttpPatch("{id}/status")]
    [Authorize(Roles = "Restaurant,Admin")]
    public async Task<IActionResult> UpdateStatus(int id, [FromBody] UpdateOrderStatusDto dto)
    {
        // Requestet fortsætter til OrderService, som tjekker ejerskab og lovlige statusskift.
        // Controlleren oversætter derefter resultatet eller fejlen til et HTTP-svar.
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var userId = User.GetId();
        var role = User.GetRole();

        bool updated;
        try
        {
            updated = await _orderService.UpdateStatusAsync(id, dto, userId, role);
        }
        catch (UnauthorizedAccessException ex)
        {
            return StatusCode(StatusCodes.Status403Forbidden, new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (KeyNotFoundException ex)
        {
            return BadRequest(new { message = ex.Message });
        }

        if (!updated)
            return NotFound();

        var order = await _orderService.GetByIdAsync(id);

        if (order is null)
            return NotFound();

        return Ok(order);
    }

    // Henter aktive ordrer for en bestemt restaurant ud fra restaurantens ID.
    [HttpGet("restaurant/{restaurantId}/active")]
    [Authorize(Roles = "Restaurant,Admin")]
    public async Task<IActionResult> GetRestaurantActiveOrdersById(
        int restaurantId)
    {
        var orders = await _orderService
            .GetRestaurantActiveOrdersByRestaurantIdAsync(restaurantId);

        return Ok(orders);
    }


    // Henter historiske ordrer for en bestemt restaurant ud fra restaurantens ID.
    [HttpGet("restaurant/{restaurantId}/history")]
    [Authorize(Roles = "Restaurant,Admin")]
    public async Task<IActionResult> GetRestaurantHistoricOrdersById(
        int restaurantId)
    {
        var orders = await _orderService
            .GetRestaurantHistoricOrdersByRestaurantIdAsync(restaurantId);

        return Ok(orders);
    }
}
