namespace Api.Services;

// Gemmer uploadede billeder lokalt på disken, under wwwroot/uploads, og serveres som statiske filer.

// IWebHostEnvironment giver adgang til WebRootPath, den fysiske sti til wwwroot-mappen på serveren.
public class LocalImageStorageService(IWebHostEnvironment env) : IImageStorageService
{   
    // Liste over tilladte filtyper (case-insensitive), alt andet afvises. 
    // OrdinalIgnoreCase, så ".JPG" og ".jpg" begge accepteres.
    private static readonly HashSet<string> AllowedExtensions =
        new(StringComparer.OrdinalIgnoreCase) { ".jpg", ".jpeg", ".png", ".webp" };

    // 5 MB, sat som en fast grænse for at undgå for store uploads.
    private const long MaxFileSizeBytes = 5 * 1024 * 1024;
    private const string UploadsUrlPrefix = "/uploads/";

    // IFormFile = en fil uploadet via HTTP-request (multipart/form-data).
    // Validerer og gemmer en uploadet fil under wwwroot/uploads/{subfolder}, og returnerer den offentlige URL.
    public async Task<string> SaveAsync(IFormFile file, string subfolder)
    {
        // Tjekker først filstørrelsen, før noget skrives til disken.
        if (file.Length == 0 || file.Length > MaxFileSizeBytes)
            throw new InvalidOperationException("File is empty or exceeds the 5 MB limit.");

        // file extension hentes fra filnavnet. 
        var extension = Path.GetExtension(file.FileName);

        // Tjekker om file extension står på listen over tilladte typer.
        if (!AllowedExtensions.Contains(extension))
            throw new InvalidOperationException("Unsupported file type. Allowed: jpg, jpeg, png, webp.");

        // Henter den fysiske sti til wwwroot-mappen; kaster en fejl, hvis den ikke er sat op.
        var webRootPath = env.WebRootPath
            ?? throw new InvalidOperationException("WebRootPath is not configured.");

        // Bygger stien til undermappen, hvor filen skal gemmes (fx wwwroot/uploads/menu-items).
        var folderPath = Path.Combine(webRootPath, "uploads", subfolder);

        // Opretter mappen, hvis den ikke findes; gør ingenting, hvis den allerede eksisterer.
        Directory.CreateDirectory(folderPath);

        // genererer 128-bit helt unikt tilfældigt ID til filnavnet.
        var fileName = $"{Guid.NewGuid()}{extension}";

        // fulde sti til filen på disken fx wwwroot/uploads/menu-items/uniktnavn.jpg
        var fullPath = Path.Combine(folderPath, fileName);

        // åbner en stream til at skrive bytes(uploadet fil) til en fil på disken.
        // opretter en ny fil på disken.
        await using (var stream = new FileStream(fullPath, FileMode.Create))
        {
            // kopierer fil-indholdet fra uploadet fil, til den nye fil på disken via stream.
            await file.CopyToAsync(stream);
        }

        // Returnerer den offentlige URL, som klienten kan bruge til at vise billedet.
        // fx: uploads/menu-items/uniktnavn.jpg
        return $"{UploadsUrlPrefix}{subfolder}/{fileName}";
    }

    
    // Sletter en tidligere gemt billedfil fra disken, men kun hvis stien rent faktisk
    // peger ind i vores egen uploads-mappe (fx ikke et eksternt billede fra en anden hjemmeside).
    public void Delete(string? relativeUrl)
    {
        // Stopper med det samme, hvis der ikke er nogen URL, eller hvis den peger uden for vores egen uploads-mappe.
        if (string.IsNullOrWhiteSpace(relativeUrl) || !relativeUrl.StartsWith(UploadsUrlPrefix, StringComparison.OrdinalIgnoreCase))
            return;

        // Henter den fysiske sti til wwwroot-mappen.
        var webRootPath = env.WebRootPath;

        // Stopper, hvis man ikke har en wwwroot-sti konfigureret.
        if (webRootPath is null)
            return;

        // fuld sti til filen på disken.
        var fullPath = Path.Combine(webRootPath, relativeUrl.TrimStart('/').Replace('/', Path.DirectorySeparatorChar));
        
        // Sletter kun filen, hvis den faktisk findes.
        if (File.Exists(fullPath))
            File.Delete(fullPath);
    }
}
