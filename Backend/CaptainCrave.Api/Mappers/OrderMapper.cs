using Api.DTOs;
using Api.Models;

namespace Api.Mappers;

// Denne klasse hjælper os med at bygge om mellem Order/OrderItem (det vi gemmer i databasen)
// og de DTO'er vi sender frem og tilbage til klienten.
public static class OrderMapper
{
    /// <summary>
    /// Tager én ordrelinje (en ret i en ordre) og pakker den om til en OrderItemDto.
    /// </summary>
    /// <returns>En OrderItemDto med rettens navn, antal og pris.</returns>
    public static OrderItemDto ToDto(this OrderItem item) => new()
    {
        Id = item.Id,
        MenuItemId = item.MenuItemId,
        MenuItemName = item.MenuItem?.Name ?? string.Empty,
        Quantity = item.Quantity,
        Price = item.Price
    };

    /// <summary>
    /// Tager en hel ordre og pakker den om til en OrderDto med alle ordrelinjer inde i sig.
    /// OBS: User, Restaurant og OrderItems skal være hentet med fra databasen først
    /// (f.eks. med Include), ellers bliver navnene tomme tekststrenge.
    /// </summary>
    /// <returns>En OrderDto med bruger-navn, restaurant-navn og listen af bestilte retter.</returns>
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

    /// <summary>
    /// Bygger en ny ordrelinje ud fra det, klienten har bestilt (hvilken ret og hvor mange).
    /// Prisen kommer IKKE fra klienten, den bliver slået op i databasen først, så en bruger
    /// ikke selv kan bestemme, hvad en ret koster.
    /// </summary>
    /// <returns>En ny OrderItem, klar til at blive gemt sammen med resten af ordren.</returns>
    public static OrderItem ToOrderItem(this CreateOrderItemDto dto, decimal price) => new()
    {
        MenuItemId = dto.MenuItemId,
        Quantity = dto.Quantity,
        Price = price
    };
}
