# CaptainCrave.Tests

Unit and integration tests for the CaptainCrave backend API.

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
  MenuControllerTests.cs
  MenuItemControllerTests.cs
  OrderControllerTests.cs
  PaymentsControllerTests.cs
  RestaurantControllerTests.cs
  ReviewControllerTests.cs
  UsersControllerTests.cs
Services/
  OrderServiceTests.cs
  PaymentServiceTests.cs
  MenuServiceTests.cs
  CategoryServiceTests.cs
  MenuItemServiceTests.cs
  RestaurantServiceTests.cs
  ReviewServiceTests.cs
  LocalImageStorageServiceTests.cs
NotificationIntegrationTests.cs
SoftDeleteIntegrationTests.cs
```

Each file in `Controllers/` mirrors its corresponding controller and follows the same pattern:

1. Create a `Mock<IXxxService>` using Moq.
2. Inject the mock into the controller under test.
3. Set up the mock to return a specific value (`.Setup(...).ReturnsAsync(...)`).
4. Call the controller method and check the result type and value.

This keeps controller tests isolated. No database, no HTTP pipeline, no real service logic.

Each file in `Services/` tests a service class directly, with its repositories (and any other
services it depends on, such as `IMenuService` for ownership checks) mocked. This catches
business logic bugs (status transition rules, ownership checks, total price calculation) closer
to the source than a controller test can, since the controller test only proves the controller
called the service correctly, not that the service itself behaves correctly.

`NotificationIntegrationTests.cs` and `SoftDeleteIntegrationTests.cs` are different: they start the
real API in memory (using `WebApplicationFactory<Program>` and an EF Core InMemory database) and
make real HTTP calls through the full pipeline (real auth, real EF Core query filters, real
SignalR hub). `SoftDeleteIntegrationTests.cs` in particular proves that EF Core's global query
filters actually hide soft-deleted rows (and cascade correctly, for example a soft-deleted menu
also hiding its categories and menu items) against a real database, something a mocked repository
can't verify.

### Soft delete tests

`RestaurantControllerTests`, `MenuControllerTests`, `CategoryControllerTests`, and `MenuItemControllerTests` also cover the soft delete endpoints on each of those controllers:

- `Delete` (soft delete) returns 204 No Content when found, 404 when not.
- `Restore` returns 204 No Content when found, 404 when not.
- `HardDelete` returns 204 No Content when found, 404 when not, and 409 Conflict when the service throws `DbUpdateException` (for example, permanently deleting a menu item that still appears on a past order).
- `GetDeleted` returns 200 OK with the trash list, or 403 Forbidden when the caller does not own the restaurant.

### Image upload tests

`RestaurantControllerTests` and `MenuItemControllerTests` cover the `POST {id}/image` upload endpoints: unauthenticated callers get 401, non-owners get 403/404, a storage validation failure (`InvalidOperationException`) becomes 400, a successful upload returns 200 with the updated DTO, and a failed authorization check after upload deletes the just-saved file.

`RestaurantServiceTests` and `MenuItemServiceTests` cover `UpdateImageUrlAsync`'s ownership checks and confirm the previous image is deleted via `IImageStorageService` when replaced.

### Mock payment tests

`PaymentServiceTests` covers the fake gateway end to end: unknown/not-awaiting-payment orders throw, a card number ending in `"0000"` fails while others succeed, the charged amount always comes from `Order.TotalPrice` (never the DTO), a successful charge updates the order to `Pending` and sends the `NewOrder` notification, and a failed charge does neither. `PaymentsControllerTests` covers the HTTP mapping (`201 Created`, `400` for invalid state/unknown order, `404` when no payment exists yet for `GetByOrderId`). `NotificationIntegrationTests` also exercises the real flow: an order stays hidden from the restaurant until `POST /api/payments` succeeds, only then does the `NewOrder` SignalR event fire.

`LocalImageStorageServiceTests.cs` exercises the real file system against a temporary web root: valid uploads are written under `wwwroot/uploads/{subfolder}` with a generated name (proving the client-supplied file name is never used, which prevents path traversal), oversized/empty files and disallowed extensions throw, and `Delete` only removes files under `/uploads/` while leaving external URLs untouched.

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
