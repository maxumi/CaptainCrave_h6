using Api.DTOs;
using Api.Models.Enums;

namespace Api.Services;

// Defines business logic operations for orders.
public interface IOrderService
{
    // Returns a single order DTO by ID, or null if not found.
    Task<OrderDto?> GetByIdAsync(int id);

    // Validates and creates an order, returning the created order as a DTO.
    Task<OrderDto> CreateAsync(CreateOrderDto dto);

    // Updates the status of an order. Returns false if the order does not exist.
    Task<bool> UpdateStatusAsync(int id, UpdateOrderStatusDto dto, int currentUserId, UserRole currentUserRole);

    // Returns active orders for the restaurant linked to the current user.
    Task<IEnumerable<OrderDto>> GetRestaurantActiveOrdersAsync(int currentUserId, UserRole currentUserRole);

    // Returns delivered/cancelled orders for the restaurant linked to the current user.
    Task<IEnumerable<OrderDto>> GetRestaurantHistoricOrdersAsync(int currentUserId, UserRole currentUserRole);

    Task<IEnumerable<OrderDto>> GetRestaurantActiveOrdersByRestaurantIdAsync(int restaurantId);

    Task<IEnumerable<OrderDto>> GetRestaurantHistoricOrdersByRestaurantIdAsync(int restaurantId);


    // Returns the active order for a user, or null if none exists.
    Task<OrderDto?> GetActiveOrderForUserAsync(int userId);

    // Returns delivered/cancelled orders for a user.
    Task<IEnumerable<OrderDto>> GetHistoricOrdersForUserAsync(int userId);

    // Checks whether the given user has a delivered order from the restaurant (used to gate review eligibility).
    Task<bool> HasCustomerOrderedFromRestaurantAsync(int userId, int restaurantId);
}