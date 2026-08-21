using Api.DTOs;
using Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Api.Controllers;

// Handles requests related to customer and restaurant orders.
[ApiController]
[Route("api/[controller]")]
public class OrdersController(IOrderService orderService) : ControllerBase
{
    private readonly IOrderService _orderService = orderService;

    // POST: api/orders — creates a new order for an authenticated customer.
    [HttpPost]
    [Authorize(Roles = "Customer,Admin")] // Only customers and admins can create orders.
    public async Task<IActionResult> Create(CreateOrderDto dto)
    {
        // Checks if the incoming request data is valid. 
        // If not, it returns a 400 Bad Request response with details about the validation errors.
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

    // GET: api/orders/{id} — returns the full order detail for any authenticated user.
    [HttpGet("{id}")]
    [Authorize]
    public async Task<IActionResult> GetById(int id)
    {
        var order = await _orderService.GetByIdAsync(id);
        if (order is null)
            return NotFound();

        return Ok(order);
    }

    // GET: api/orders/active — returns the active order for the current user.
    [HttpGet("customer/active")]
    [Authorize(Roles = "Customer")]
    public async Task<IActionResult> GetActiveOrderForCustomer()
    {
        // Gets the authenticated user's ID from the JWT token.
        var userId = User.GetId();

        // Retrieves the customer's active order. If there is no active order, it returns a 404 Not Found response.
        var order = await _orderService.GetActiveOrderForUserAsync(userId);

        if (order is null)
            return NotFound();

        return Ok(order);
    }

    // Backward-compatible route alias for existing clients.
    [HttpGet("active")]
    [Authorize(Roles = "Customer")]
    public async Task<IActionResult> GetActiveOrder() => await GetActiveOrderForCustomer();

    // Returns all completed or historical orders for a customer.
    [HttpGet("customer/history")]
    [Authorize(Roles = "Customer")]
    public async Task<IActionResult> GetCustomerHistory()
    {
        // Gets the current customer ID from JWT.
        var userId = User.GetId();

        // Retrieves previous orders. If there are no previous orders, it returns an empty list.
        var orders = await _orderService.GetHistoricOrdersForUserAsync(userId);
        return Ok(orders);
    }

    // Returns all active orders belonging to a restaurant.
    [HttpGet("restaurant/active")]
    [Authorize(Roles = "Restaurant,Admin")]
    public async Task<IActionResult> GetRestaurantActiveOrders()
    {
        var userId = User.GetId();
        var role = User.GetRole();
        try
        {
            // Retrieves active orders for the restaurant.
            var orders = await _orderService.GetRestaurantActiveOrdersAsync(userId, role);
            return Ok(orders);
        }
        catch (UnauthorizedAccessException ex)
        {
            // User is not allowed to access these orders.
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

    // Returns all completed orders for a restaurant.
    [HttpGet("restaurant/history")]
    [Authorize(Roles = "Restaurant,Admin")]
    public async Task<IActionResult> GetRestaurantHistoricOrders()
    {
        var userId = User.GetId();
        var role = User.GetRole();
        try
        {
            // Retrieves completed restaurant orders.
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
    // Updates the status of an order. 
    // Only restaurant users can update to "Preparing", "Ready", or "Completed". 
    // Only customers can update to "Cancelled". 
    [HttpPatch("{id}/status")]
    [Authorize(Roles = "Restaurant,Admin")]
    public async Task<IActionResult> UpdateStatus(int id, [FromBody] UpdateOrderStatusDto dto)
    {
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

    [HttpGet("restaurant/{restaurantId}/active")]
    [Authorize(Roles = "Restaurant,Admin")]
    public async Task<IActionResult> GetRestaurantActiveOrdersById(
        int restaurantId)
    {
        var orders = await _orderService
            .GetRestaurantActiveOrdersByRestaurantIdAsync(restaurantId);

        return Ok(orders);
    }
    
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