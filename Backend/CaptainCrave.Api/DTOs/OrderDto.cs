using Api.Models.Enums;

namespace Api.DTOs;

public class OrderDto
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public string UserName { get; set; } = string.Empty;
    public string UserEmail { get; set; } = string.Empty;
    public int RestaurantId { get; set; }
    public string RestaurantName { get; set; } = string.Empty;
    public OrderStatus Status { get; set; }
    public DeliveryType DeliveryType { get; set; }
    public string? DeliveryAddress { get; set; }
    public decimal TotalPrice { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public IEnumerable<OrderItemDto> Items { get; set; } = [];
}
