namespace Api.Services;

// Stores uploaded images on local disk, under wwwroot/uploads, served back via static files.
public class LocalImageStorageService(IWebHostEnvironment env) : IImageStorageService
{
    private static readonly HashSet<string> AllowedExtensions =
        new(StringComparer.OrdinalIgnoreCase) { ".jpg", ".jpeg", ".png", ".webp" };

    private const long MaxFileSizeBytes = 5 * 1024 * 1024;
    private const string UploadsUrlPrefix = "/uploads/";

    public async Task<string> SaveAsync(IFormFile file, string subfolder)
    {
        if (file.Length == 0 || file.Length > MaxFileSizeBytes)
            throw new InvalidOperationException("File is empty or exceeds the 5 MB limit.");

        var extension = Path.GetExtension(file.FileName);
        if (!AllowedExtensions.Contains(extension))
            throw new InvalidOperationException("Unsupported file type. Allowed: jpg, jpeg, png, webp.");

        var webRootPath = env.WebRootPath
            ?? throw new InvalidOperationException("WebRootPath is not configured.");

        var folderPath = Path.Combine(webRootPath, "uploads", subfolder);
        Directory.CreateDirectory(folderPath);

        // generate the file name ourselves; never trust the client-supplied name
        var fileName = $"{Guid.NewGuid()}{extension}";
        var fullPath = Path.Combine(folderPath, fileName);

        await using (var stream = new FileStream(fullPath, FileMode.Create))
        {
            await file.CopyToAsync(stream);
        }

        return $"{UploadsUrlPrefix}{subfolder}/{fileName}";
    }

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
