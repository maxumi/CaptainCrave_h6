using System.Security.Claims;
using Api.Controllers;
using Api.DTOs;
using Api.Models;
using Api.Models.Enums;
using Api.Repositories;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;

namespace Api.Tests.Controllers;

// Unit-tests for UsersController.
// IUserRepository bliver mocket, så der ikke sker nogen database-adgang.
public class UsersControllerTests
{
    // Opretter en UsersController med et mocket IUserRepository og en autentificeret HttpContext (bruger-id "1").
    private static (UsersController controller, Mock<IUserRepository> mockRepository) CreateController()
    {
        var mockRepository = new Mock<IUserRepository>();
        var controller = new UsersController(mockRepository.Object);
        var claims = new[] { new Claim(ClaimTypes.NameIdentifier, "1") };
        var user = new ClaimsPrincipal(new ClaimsIdentity(claims, "Test"));
        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = user }
        };
        return (controller, mockRepository);
    }

    // Opretter en UsersController uden noget bruger-id-claim, så GetCurrentUserId bliver null.
    private static UsersController CreateUnauthenticatedController()
    {
        var controller = new UsersController(new Mock<IUserRepository>().Object);
        var user = new ClaimsPrincipal(new ClaimsIdentity());
        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = user }
        };
        return controller;
    }

    // GetMe

    // Henter den logget-ind brugers profil og giver 200 OK.
    [Fact]
    public async Task GetMe_ExistingUser_ReturnsOk()
    {
        var (controller, mockRepository) = CreateController();
        var user = new User { Id = 1, Name = "Alice", Email = "alice@example.com", Address = "Main St", Role = UserRole.Customer };
        mockRepository.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(user);

        var result = await controller.GetMe();

        Assert.IsType<OkObjectResult>(result);
    }

    // Svaret indeholder brugerens navn, e-mail og rolle.
    [Fact]
    public async Task GetMe_ExistingUser_ReturnsUserProfile()
    {
        var (controller, mockRepository) = CreateController();
        var user = new User { Id = 1, Name = "Alice", Email = "alice@example.com", Address = "Main St", Role = UserRole.Customer };
        mockRepository.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(user);

        var result = await controller.GetMe() as OkObjectResult;
        var profile = result?.Value as UserProfileDto;

        Assert.Equal(user.Id, profile?.Id);
        Assert.Equal(user.Name, profile?.Name);
        Assert.Equal(user.Email, profile?.Email);
        Assert.Equal("Customer", profile?.Role);
    }

    // Hvis brugeren ikke findes i databasen, giver det 404 Not Found.
    [Fact]
    public async Task GetMe_UserNotFound_ReturnsNotFound()
    {
        var (controller, mockRepository) = CreateController();
        mockRepository.Setup(r => r.GetByIdAsync(1)).ReturnsAsync((User?)null);

        var result = await controller.GetMe();

        Assert.IsType<NotFoundResult>(result);
    }

    // Ikke logget ind giver 401 Unauthorized.
    [Fact]
    public async Task GetMe_NoUserIdClaim_ReturnsUnauthorized()
    {
        var controller = CreateUnauthenticatedController();

        var result = await controller.GetMe();

        Assert.IsType<UnauthorizedResult>(result);
    }

    // UpdateMe

    // Opdaterer profilen med gyldig data og giver 200 OK.
    [Fact]
    public async Task UpdateMe_ValidDto_ReturnsOk()
    {
        var (controller, mockRepository) = CreateController();
        var dto = new UpdateUserProfileDto { Name = "Alice Updated", Address = "New St", Latitude = 55.6, Longitude = 12.5 };
        var updated = new User { Id = 1, Name = "Alice Updated", Email = "alice@example.com", Address = "New St", Latitude = 55.6, Longitude = 12.5, Role = UserRole.Customer };

        mockRepository.Setup(r => r.UpdateProfileAsync(1, dto.Name, dto.Address, dto.Latitude, dto.Longitude)).ReturnsAsync(updated);

        var result = await controller.UpdateMe(dto);

        Assert.IsType<OkObjectResult>(result);
    }

    // Svaret indeholder de opdaterede felter (navn og adresse).
    [Fact]
    public async Task UpdateMe_ValidDto_ReturnsUpdatedProfile()
    {
        var (controller, mockRepository) = CreateController();
        var dto = new UpdateUserProfileDto { Name = "Alice Updated", Address = "New St", Latitude = 55.6, Longitude = 12.5 };
        var updated = new User { Id = 1, Name = "Alice Updated", Email = "alice@example.com", Address = "New St", Latitude = 55.6, Longitude = 12.5, Role = UserRole.Customer };

        mockRepository.Setup(r => r.UpdateProfileAsync(1, dto.Name, dto.Address, dto.Latitude, dto.Longitude)).ReturnsAsync(updated);

        var result = await controller.UpdateMe(dto) as OkObjectResult;
        var profile = result?.Value as UserProfileDto;

        Assert.Equal(updated.Name, profile?.Name);
        Assert.Equal(updated.Address, profile?.Address);
    }

    // Ugyldig model-state giver 400 Bad Request.
    [Fact]
    public async Task UpdateMe_InvalidModelState_ReturnsBadRequest()
    {
        var (controller, _) = CreateController();
        controller.ModelState.AddModelError("Name", "Required");

        var result = await controller.UpdateMe(new UpdateUserProfileDto());

        Assert.IsType<BadRequestObjectResult>(result);
    }

    // Hvis brugeren ikke findes, giver det 404 Not Found.
    [Fact]
    public async Task UpdateMe_UserNotFound_ReturnsNotFound()
    {
        var (controller, mockRepository) = CreateController();
        var dto = new UpdateUserProfileDto { Name = "Alice", Address = "Main St" };

        mockRepository.Setup(r => r.UpdateProfileAsync(1, dto.Name, dto.Address, dto.Latitude, dto.Longitude)).ReturnsAsync((User?)null);

        var result = await controller.UpdateMe(dto);

        Assert.IsType<NotFoundResult>(result);
    }

    // Ikke logget ind giver 401 Unauthorized.
    [Fact]
    public async Task UpdateMe_NoUserIdClaim_ReturnsUnauthorized()
    {
        var controller = CreateUnauthenticatedController();

        var result = await controller.UpdateMe(new UpdateUserProfileDto { Name = "Alice", Address = "Main St" });

        Assert.IsType<UnauthorizedResult>(result);
    }
}
