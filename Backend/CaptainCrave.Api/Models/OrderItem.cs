namespace Api.Models;

public class OrderItem : IAuditable
{
    public int Id { get; set; }

    public int OrderId { get; set; }

    public Order Order { get; set; } = null!;

    public int MenuItemId { get; set; }

    public MenuItem MenuItem { get; set; } = null!;

    public int Quantity { get; set; }

    public decimal Price { get; set; }

    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}