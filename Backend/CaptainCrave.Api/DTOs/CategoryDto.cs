namespace Api.DTOs;

public class CategoryDto
{
    public int Id { get; set; }
    public int MenuId { get; set; }
    public string Name { get; set; } = string.Empty;

    public bool IsDeleted { get; set; }
    public DateTime? DeletedAt { get; set; }

    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}
