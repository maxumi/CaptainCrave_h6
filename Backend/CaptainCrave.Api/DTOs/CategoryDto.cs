namespace Api.DTOs;

// Repræsenterer en kategori, som returneres fra API'et.
public class CategoryDto
{
    public int Id { get; set; }
    public int MenuId { get; set; }
    public string Name { get; set; } = string.Empty;

    // Soft delete-oplysninger.
    public bool IsDeleted { get; set; }
    public DateTime? DeletedAt { get; set; }

    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}
