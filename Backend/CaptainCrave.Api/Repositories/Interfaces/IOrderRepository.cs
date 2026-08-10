using Api.Models;
using Api.Models.Enums;

namespace Api.Repositories;

// Defines data access operations for orders.
public interface IOrderRepository
{
    // Returns an order with all navigation properties loaded, or null if not found.
    Task<Order?> GetByIdAsync(int id);

    // Saves a new order (with its items) and returns it with the generated ID.
    Task<Order> CreateAsync(Order order);

    // Updates the status and updated_at of an existing order. Returns false if not found.
    Task<bool> UpdateStatusAsync(int id, OrderStatus status);

    // Returns active orders for a restaurant.
    Task<IEnumerable<Order>> GetActiveByRestaurantAsync(int restaurantId);

    // Returns delivered/cancelled orders for a restaurant.
    Task<IEnumerable<Order>> GetHistoryByRestaurantAsync(int restaurantId);

    // Returns the first active order for a user, or null if none exist.
    Task<Order?> GetActiveOrderForUserAsync(int userId);

    // Returns delivered/cancelled orders for a user.
    Task<IEnumerable<Order>> GetHistoryForUserAsync(int userId);
}
