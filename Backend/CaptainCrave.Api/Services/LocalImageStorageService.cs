namespace Api.Services;

// Gemmer uploadede billeder lokalt på disken, under wwwroot/uploads, og serveres som statiske filer.
public class LocalImageStorageService(IWebHostEnvironment env) : IImageStorageService
{
    private static readonly HashSet<string> AllowedExtensions =
        new(StringComparer.OrdinalIgnoreCase) { ".jpg", ".jpeg", ".png", ".webp" };

    private const long MaxFileSizeBytes = 5 * 1024 * 1024;
    private const string UploadsUrlPrefix = "/uploads/";

    /// <summary>
    /// Gemmer en uploadet fil på disken under et helt nyt, tilfældigt navn.
    /// Vi bruger ALDRIG det filnavn, brugeren selv har sendt, fordi det kan indeholde
    /// farlige tegn eller forsøge at overskrive andre filer på serveren.
    /// </summary>
    /// <returns>Den relative URL til den gemte fil, fx "/uploads/menu-items/xxx.jpg".</returns>
    public async Task<string> SaveAsync(IFormFile file, string subfolder)
    {
        // Filer er input fra brugeren og må ikke stoles på.
        // Størrelse og filendelse kontrolleres, før noget skrives til disken.
        if (file.Length == 0 || file.Length > MaxFileSizeBytes)
            throw new InvalidOperationException("File is empty or exceeds the 5 MB limit.");

        var extension = Path.GetExtension(file.FileName);
        if (!AllowedExtensions.Contains(extension))
            throw new InvalidOperationException("Unsupported file type. Allowed: jpg, jpeg, png, webp.");

        var webRootPath = env.WebRootPath
            ?? throw new InvalidOperationException("WebRootPath is not configured.");

        var folderPath = Path.Combine(webRootPath, "uploads", subfolder);
        Directory.CreateDirectory(folderPath);

        // GUID-navnet undgår navnekollisioner og path traversal gennem brugerens filnavn.
        var fileName = $"{Guid.NewGuid()}{extension}";
        var fullPath = Path.Combine(folderPath, fileName);

        await using (var stream = new FileStream(fullPath, FileMode.Create))
        {
            await file.CopyToAsync(stream);
        }

        return $"{UploadsUrlPrefix}{subfolder}/{fileName}";
    }

    // Sletter en tidligere gemt billedfil fra disken, men kun hvis stien rent faktisk
    // peger ind i vores egen uploads-mappe. Gør ingenting, hvis stien er tom, eller hvis
    // den peger på noget udenfor (fx et eksternt billede fra en anden hjemmeside).
    public void Delete(string? relativeUrl)
    {
        if (string.IsNullOrWhiteSpace(relativeUrl) || !relativeUrl.StartsWith(UploadsUrlPrefix, StringComparison.OrdinalIgnoreCase))
            return;

        var webRootPath = env.WebRootPath;
        if (webRootPath is null)
            return;

        var fullPath = Path.Combine(webRootPath, relativeUrl.TrimStart('/').Replace('/', Path.DirectorySeparatorChar));
        if (File.Exists(fullPath))
            File.Delete(fullPath);
    }
}
