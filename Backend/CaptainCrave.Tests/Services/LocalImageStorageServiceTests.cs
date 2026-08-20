using System.Text;
using Api.Services;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Moq;

namespace Api.Tests.Services;

// Unit tests for LocalImageStorageService. Uses a real temp directory as the web root,
// since the service performs actual file system I/O.
public class LocalImageStorageServiceTests : IDisposable
{
    private readonly string _webRootPath;
    private readonly LocalImageStorageService _service;

    public LocalImageStorageServiceTests()
    {
        _webRootPath = Path.Combine(Path.GetTempPath(), "CaptainCraveTests_" + Guid.NewGuid());
        Directory.CreateDirectory(_webRootPath);

        var mockEnv = new Mock<IWebHostEnvironment>();
        mockEnv.Setup(e => e.WebRootPath).Returns(_webRootPath);
        _service = new LocalImageStorageService(mockEnv.Object);
    }

    public void Dispose()
    {
        if (Directory.Exists(_webRootPath))
            Directory.Delete(_webRootPath, recursive: true);
    }

    private static IFormFile CreateFormFile(string fileName, int contentLength = 10)
    {
        var content = Encoding.UTF8.GetBytes(new string('a', contentLength));
        var stream = new MemoryStream(content);
        return new FormFile(stream, 0, content.Length, "file", fileName);
    }

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

    [Fact]
    public async Task SaveAsync_GeneratesNameIndependentOfClientFileName()
    {
        var file = CreateFormFile("../../evil.jpg");

        var relativeUrl = await _service.SaveAsync(file, "restaurants");

        Assert.DoesNotContain("evil", relativeUrl);
        Assert.DoesNotContain("..", relativeUrl);
    }

    [Fact]
    public async Task SaveAsync_EmptyFile_ThrowsInvalidOperationException()
    {
        var file = CreateFormFile("photo.jpg", contentLength: 0);

        await Assert.ThrowsAsync<InvalidOperationException>(() => _service.SaveAsync(file, "restaurants"));
    }

    [Fact]
    public async Task SaveAsync_DisallowedExtension_ThrowsInvalidOperationException()
    {
        var file = CreateFormFile("document.pdf");

        await Assert.ThrowsAsync<InvalidOperationException>(() => _service.SaveAsync(file, "restaurants"));
    }

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

    [Fact]
    public void Delete_ExternalUrl_DoesNotThrow()
    {
        var exception = Record.Exception(() => _service.Delete("https://upload.wikimedia.org/logo.svg"));

        Assert.Null(exception);
    }

    [Fact]
    public void Delete_NullOrEmpty_DoesNotThrow()
    {
        var exception = Record.Exception(() => _service.Delete(null));

        Assert.Null(exception);
    }
}
