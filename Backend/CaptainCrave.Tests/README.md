# CaptainCrave.Tests

Unit tests for the CaptainCrave backend API controllers.

## Tech Stack

| Package | Purpose |
|---|---|
| **xUnit** | Test framework. Discovers and runs tests. |
| **Moq** | Mocking library. Replaces real service dependencies with fakes. |
| **Microsoft.AspNetCore.Mvc.Testing** | Provides ASP.NET Core types (for example `OkObjectResult`) without spinning up a real server. |
| **Microsoft.EntityFrameworkCore.InMemory** | In-memory database provider for tests that need a real `DbContext`. |
| **Microsoft.AspNetCore.SignalR.Client** | SignalR client used to test the notification hub end to end. |

## Structure

```
Controllers/
  AuthControllerTests.cs
  CategoryControllerTests.cs
  MenuItemControllerTests.cs
  OrderControllerTests.cs
  RestaurantControllerTests.cs
NotificationIntegrationTests.cs
```

Each file in `Controllers/` mirrors its corresponding controller and follows the same pattern:

1. Create a `Mock<IXxxService>` using Moq.
2. Inject the mock into the controller under test.
3. Set up the mock to return a specific value (`.Setup(...).ReturnsAsync(...)`).
4. Call the controller method and check the result type and value.

This keeps controller tests isolated. No database, no HTTP pipeline, no real service logic.

`NotificationIntegrationTests.cs` is different: it starts the real API in memory (using `WebApplicationFactory<Program>` and an EF Core InMemory database) and connects a real SignalR client to check that order events are actually delivered.

## Why `[Fact]`?

xUnit offers two attributes for test methods:

- **`[Fact]`**: a single, standalone test with no parameters. Used here because each scenario has its own fixed inputs and expected outputs, which makes the test easy to read and debug on its own.
- **`[Theory]`**: a parameterized test that runs the same logic with multiple data sets, using `[InlineData]` or `[MemberData]`. Useful when the same behavior needs to be checked across many inputs.

All tests in this project use `[Fact]`, because each case has its own setup (different mock return values, different model state errors) that would not gain much from parameterization.

## Running the Tests

```bash
dotnet test
```

Run a specific file:

```bash
dotnet test --filter "FullyQualifiedName~RestaurantControllerTests"
```
