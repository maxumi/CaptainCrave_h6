using Api.Models.Enums;

namespace Api.Models;

public class Order
{
    public int Id { get; set; }

    public int UserId { get; set; }

    public User User { get; set; } = null!;

    public int RestaurantId { get; set; }

    public Restaurant Restaurant { get; set; } = null!;

    public OrderStatus Status { get; set; } = OrderStatus.Pending;

    public DeliveryType DeliveryType { get; set; }

    public string? DeliveryAddress { get; set; }

    public decimal TotalPrice { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<OrderItem> OrderItems { get; set; }
        = new List<OrderItem>();
}