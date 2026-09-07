namespace Api.Services;

// Definerer funktioner til lagring og sletning af uploadede billeder.
public interface IImageStorageService
{
    // Gemmer filen under wwwroot/uploads/{subfolder} med et genereret filnavn
    // og returnerer den relative URL til billedet.
    Task<string> SaveAsync(IFormFile file, string subfolder);

    // Sletter et tidligere gemt billede ud fra den relative URL fra SaveAsync.
    // Eksterne URL'er og ugyldige værdier ignoreres.
    void Delete(string? relativeUrl);
}
