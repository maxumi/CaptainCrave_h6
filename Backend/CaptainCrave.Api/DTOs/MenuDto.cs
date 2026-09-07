namespace Api.DTOs;

// Repræsenterer en menu, som returneres fra API'et.
public class MenuDto
{
    // Menuens unikke ID.
    public int Id { get; set; }

    // ID på restauranten som menuen tilhører.
    public int RestaurantId { get; set; }

    // Navnet der vises for menuen.
    public string Name { get; set; } = string.Empty;

    // Angiver om menuen er soft-deleted og derfor skjult, men stadig kan gendannes.
    public bool IsDeleted { get; set; }
    public DateTime? DeletedAt { get; set; }

    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}
