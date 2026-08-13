using Api.Controllers;
using Api.DTOs.Auth;
using Api.Services;
using Microsoft.AspNetCore.Mvc;
using Moq;

namespace Api.Tests.Controllers;

// Unit tests for AuthController.
// IAuthService is mocked so no database, password hashing, or JWT logic runs.
public class AuthControllerTests
{
    // Creates an AuthController with a mocked IAuthService.
    private static (AuthController controller, Mock<IAuthService> mockService) CreateController()
    {
        var mockService = new Mock<IAuthService>();
        var controller = new AuthController(mockService.Object);
        return (controller, mockService);
    }

    // Register

    // Successful registration returns 201 Created.
    [Fact]
    public async Task Register_ValidDto_ReturnsCreatedAtAction()
    {
        var (controller, mockService) = CreateController();
        var dto = new RegisterRequestDto { Name = "Alice", Email = "alice@example.com", Password = "password123" };
        var response = new AuthResponseDto { Id = 1, Name = "Alice", Email = "alice@example.com", Role = "Customer", Token = "token" };

        mockService.Setup(s => s.RegisterAsync(dto)).ReturnsAsync(response);

        var result = await controller.Register(dto);

        Assert.IsType<CreatedAtActionResult>(result);
    }

    // Response body contains the auth token and user info.
    [Fact]
    public async Task Register_ValidDto_ReturnsAuthResponse()
    {
        var (controller, mockService) = CreateController();
        var dto = new RegisterRequestDto { Name = "Alice", Email = "alice@example.com", Password = "password123" };
        var response = new AuthResponseDto { Id = 1, Name = "Alice", Email = "alice@example.com", Role = "Customer", Token = "token" };

        mockService.Setup(s => s.RegisterAsync(dto)).ReturnsAsync(response);

        var result = await controller.Register(dto) as CreatedAtActionResult;

        Assert.Equal(response, result?.Value);
    }

    // Duplicate email causes the service to throw InvalidOperationException, which the controller maps to 409 Conflict.
    [Fact]
    public async Task Register_DuplicateEmail_ReturnsConflict()
    {
        var (controller, mockService) = CreateController();
        var dto = new RegisterRequestDto { Name = "Alice", Email = "alice@example.com", Password = "password123" };

        mockService.Setup(s => s.RegisterAsync(dto)).ThrowsAsync(new InvalidOperationException("Email is already in use."));

        var result = await controller.Register(dto);

        Assert.IsType<ConflictObjectResult>(result);
    }

    // Invalid model state short-circuits before calling the service and returns 400 Bad Request.
    [Fact]
    public async Task Register_InvalidModelState_ReturnsBadRequest()
    {
        var (controller, mockService) = CreateController();
        controller.ModelState.AddModelError("Email", "Required");

        var result = await controller.Register(new RegisterRequestDto());

        Assert.IsType<BadRequestObjectResult>(result);
    }

    // Login

    // Valid credentials return 200 OK.
    [Fact]
    public async Task Login_ValidCredentials_ReturnsOk()
    {
        var (controller, mockService) = CreateController();
        var dto = new LoginRequestDto { Email = "alice@example.com", Password = "password123" };
        var response = new AuthResponseDto { Id = 1, Name = "Alice", Email = "alice@example.com", Role = "Customer", Token = "token" };

        mockService.Setup(s => s.LoginAsync(dto)).ReturnsAsync(response);

        var result = await controller.Login(dto);

        Assert.IsType<OkObjectResult>(result);
    }

    // Response body contains the auth token and user info.
    [Fact]
    public async Task Login_ValidCredentials_ReturnsAuthResponse()
    {
        var (controller, mockService) = CreateController();
        var dto = new LoginRequestDto { Email = "alice@example.com", Password = "password123" };
        var response = new AuthResponseDto { Id = 1, Name = "Alice", Email = "alice@example.com", Role = "Customer", Token = "token" };

        mockService.Setup(s => s.LoginAsync(dto)).ReturnsAsync(response);

        var result = await controller.Login(dto) as OkObjectResult;

        Assert.Equal(response, result?.Value);
    }

    // Wrong password causes the service to throw UnauthorizedAccessException, which maps to 401 Unauthorized.
    [Fact]
    public async Task Login_InvalidCredentials_ReturnsUnauthorized()
    {
        var (controller, mockService) = CreateController();
        var dto = new LoginRequestDto { Email = "alice@example.com", Password = "wrongpassword" };

        mockService.Setup(s => s.LoginAsync(dto)).ThrowsAsync(new UnauthorizedAccessException());

        var result = await controller.Login(dto);

        Assert.IsType<UnauthorizedObjectResult>(result);
    }

    // Invalid model state short-circuits before calling the service and returns 400 Bad Request.
    [Fact]
    public async Task Login_InvalidModelState_ReturnsBadRequest()
    {
        var (controller, mockService) = CreateController();
        controller.ModelState.AddModelError("Email", "Required");

        var result = await controller.Login(new LoginRequestDto());

        Assert.IsType<BadRequestObjectResult>(result);
    }
}
