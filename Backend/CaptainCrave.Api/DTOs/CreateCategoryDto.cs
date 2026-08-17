using System.ComponentModel.DataAnnotations;

namespace Api.DTOs;

public class CreateCategoryDto
{
    [Required]
    public int MenuId { get; set; }

    [Required]
    [MaxLength(100)]
    public string Name { get; set; } = string.Empty;
}
