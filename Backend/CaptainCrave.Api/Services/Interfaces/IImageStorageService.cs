namespace Api.Services;

// Defines operations for storing and removing uploaded images.
public interface IImageStorageService
{
    // Saves the file under wwwroot/uploads/{subfolder} with a generated name and returns its public relative URL.
    Task<string> SaveAsync(IFormFile file, string subfolder);

    // Deletes a previously stored image given the relative URL returned by SaveAsync. No-op for anything else (e.g. external URLs).
    void Delete(string? relativeUrl);
}
