using Api.DTOs;
using Api.Models;

namespace Api.Mappers;

// Extension methods for mapping between Order/OrderItem models and DTOs.
public static class OrderMapper
{
    // Maps an OrderItem entity to an OrderItemDto for API responses.
    public static OrderItemDto ToDto(this OrderItem item) => new()
    {
        Id = item.Id,
        MenuItemId = item.MenuItemId,
        MenuItemName = item.MenuItem?.Name ?? string.Empty,
        Quantity = item.Quantity,
        Price = item.Price
    };

    // Maps an Order entity (with navigation properties loaded) to an OrderDto.
    public static OrderDto ToDto(this Order order) => new()
    {
        Id = order.Id,
        UserId = order.UserId,
        UserName = order.User?.Name ?? string.Empty,
        UserEmail = order.User?.Email ?? string.Empty,
        RestaurantId = order.RestaurantId,
        RestaurantName = order.Restaurant?.Name ?? string.Empty,
        Status = order.Status,
        DeliveryType = order.DeliveryType,
        DeliveryAddress = order.DeliveryAddress,
        TotalPrice = order.TotalPrice,
        CreatedAt = order.CreatedAt,
        UpdatedAt = order.UpdatedAt,
        Items = order.OrderItems.Select(i => i.ToDto())
    };

    // Maps a CreateOrderItemDto and a resolved MenuItem price to an OrderItem entity.
    public static OrderItem ToOrderItem(this CreateOrderItemDto dto, decimal price) => new()
    {
        MenuItemId = dto.MenuItemId,
        Quantity = dto.Quantity,
        Price = price
    };
}
