using System.ComponentModel.DataAnnotations;

namespace Api.DTOs;

// Data der bruges til at oprette en ny kategori.
public class CreateCategoryDto
{
    [Required]
    public int MenuId { get; set; }

    [Required]
    [MaxLength(100)]
    public string Name { get; set; } = string.Empty;
}
