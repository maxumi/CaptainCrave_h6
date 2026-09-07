namespace Api.DTOs;

// Data der bruges til at ændre vurderingen på en eksisterende anmeldelse.
public class UpdateReviewDto
{
    // Den nye vurdering forventes at være mellem 1 og 5.
    public int Rating { get; set; }
}