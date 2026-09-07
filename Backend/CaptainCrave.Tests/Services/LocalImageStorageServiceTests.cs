using System.Text;
using Api.Services;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Moq;

namespace Api.Tests.Services;

// Unit-tests for LocalImageStorageService. Bruger et rigtigt midlertidigt katalog som web-rod,
// da servicen udfører rigtig fil-I/O.
public class LocalImageStorageServiceTests : IDisposable
{
    private readonly string _webRootPath;
    private readonly LocalImageStorageService _service;

    // Opretter en tom, midlertidig webmappe og peger billedservicen på den.
    public LocalImageStorageServiceTests()
    {
        _webRootPath = Path.Combine(Path.GetTempPath(), "CaptainCraveTests_" + Guid.NewGuid());
        Directory.CreateDirectory(_webRootPath);

        var mockEnv = new Mock<IWebHostEnvironment>();
        mockEnv.Setup(e => e.WebRootPath).Returns(_webRootPath);
        _service = new LocalImageStorageService(mockEnv.Object);
    }

    // Rydder den midlertidige webmappe op, når hver test er færdig.
    public void Dispose()
    {
        if (Directory.Exists(_webRootPath))
            Directory.Delete(_webRootPath, recursive: true);
    }

    /// <summary>Bygger en lille falsk uploadfil direkte i hukommelsen.</summary>
    /// <returns>En formularfil, der kan sendes til billedservicen.</returns>
    private static IFormFile CreateFormFile(string fileName, int contentLength = 10)
    {
        var content = Encoding.UTF8.GetBytes(new string('a', contentLength));
        var stream = new MemoryStream(content);
        return new FormFile(stream, 0, content.Length, "file", fileName);
    }

    // Gemmer en gyldig fil under den rigtige undermappe og giver en URL, man kan bruge i browseren.
    [Fact]
    public async Task SaveAsync_ValidFile_WritesFileUnderSubfolderAndReturnsRelativeUrl()
    {
        var file = CreateFormFile("photo.jpg");

        var relativeUrl = await _service.SaveAsync(file, "restaurants");

        Assert.StartsWith("/uploads/restaurants/", relativeUrl);
        Assert.EndsWith(".jpg", relativeUrl);
        var fullPath = Path.Combine(_webRootPath, relativeUrl.TrimStart('/').Replace('/', Path.DirectorySeparatorChar));
        Assert.True(File.Exists(fullPath));
    }

    // Filnavnet, klienten sender, bliver aldrig brugt direkte, så man ikke kan snyde med farlige stier.
    [Fact]
    public async Task SaveAsync_GeneratesNameIndependentOfClientFileName()
    {
        var file = CreateFormFile("../../evil.jpg");

        var relativeUrl = await _service.SaveAsync(file, "restaurants");

        Assert.DoesNotContain("evil", relativeUrl);
        Assert.DoesNotContain("..", relativeUrl);
    }

    // En tom fil bliver afvist med en fejl.
    [Fact]
    public async Task SaveAsync_EmptyFile_ThrowsInvalidOperationException()
    {
        var file = CreateFormFile("photo.jpg", contentLength: 0);

        await Assert.ThrowsAsync<InvalidOperationException>(() => _service.SaveAsync(file, "restaurants"));
    }

    // En filtype, der ikke er tilladt (fx PDF), bliver afvist med en fejl.
    [Fact]
    public async Task SaveAsync_DisallowedExtension_ThrowsInvalidOperationException()
    {
        var file = CreateFormFile("document.pdf");

        await Assert.ThrowsAsync<InvalidOperationException>(() => _service.SaveAsync(file, "restaurants"));
    }

    // Sletter en fil, der ligger lokalt under uploads-mappen.
    [Fact]
    public void Delete_LocalUploadUrl_DeletesFile()
    {
        var folder = Path.Combine(_webRootPath, "uploads", "restaurants");
        Directory.CreateDirectory(folder);
        var filePath = Path.Combine(folder, "existing.jpg");
        File.WriteAllText(filePath, "data");

        _service.Delete("/uploads/restaurants/existing.jpg");

        Assert.False(File.Exists(filePath));
    }

    // En URL til et eksternt billede (fx et logo) bliver ignoreret, uden at det giver en fejl.
    [Fact]
    public void Delete_ExternalUrl_DoesNotThrow()
    {
        var exception = Record.Exception(() => _service.Delete("https://upload.wikimedia.org/logo.svg"));

        Assert.Null(exception);
    }

    // Et tomt eller manglende billede-link giver ingen fejl, det bliver bare sprunget over.
    [Fact]
    public void Delete_NullOrEmpty_DoesNotThrow()
    {
        var exception = Record.Exception(() => _service.Delete(null));

        Assert.Null(exception);
    }
}
