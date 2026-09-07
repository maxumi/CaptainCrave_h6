namespace Api.DTOs;

// Repræsenterer en enkelt order-item.
public class OrderItemDto
{
    public int Id { get; set; }
    public int MenuItemId { get; set; }
    public string MenuItemName { get; set; } = string.Empty;
    public int Quantity { get; set; }

    // Prisen pr. enhed på tidspunktet hvor ordren blev oprettet.
    public decimal Price { get; set; }

    // Beregner den samlede pris for order-item.
    public decimal LineTotal => Price * Quantity;
}
