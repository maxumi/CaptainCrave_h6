namespace Api.DTOs;

public class CategoryDto
{
    public int Id { get; set; }
    public int RestaurantId { get; set; }
    public string Name { get; set; } = string.Empty;
}
