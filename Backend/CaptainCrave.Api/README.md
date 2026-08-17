# CaptainCrave.Api

ASP.NET Core Web API for CaptainCrave - a food delivery platform connecting customers with restaurants.

## Tech stack

- **ASP.NET Core 10** (Web API, Controllers)
- **Entity Framework Core** with SQL Server
- **JWT Bearer** authentication (custom `TokenService`, BCrypt password hashing)
- **Swagger / OpenAPI** (available at `/swagger` in Development)

## Project structure

```
Controllers/    HTTP endpoints (Auth, Users, Restaurants, Menus, Categories, MenuItems, Orders)
Services/       Business logic
Repositories/   EF Core data access
Models/         Database entities
DTOs/           Request/response payloads
Mappers/        Entity <-> DTO conversions
Data/           AppDbContext + EF Core fluent configurations
Migrations/     EF Core migrations
```

## Setup guide

### Prerequisites

- .NET 10 SDK
- SQL Server (LocalDB, a full instance, or a container) reachable from your machine
- EF Core CLI tools: `dotnet tool install --global dotnet-ef` (skip if already installed)

### 1. Configure user secrets

Run these commands from this folder (`Backend/CaptainCrave.Api`):

```bash
dotnet user-secrets init

dotnet user-secrets set "Jwt:Secret" "your-super-secret-key-at-least-32-characters-long"
dotnet user-secrets set "Jwt:Issuer" "CaptainCrave.Api"
dotnet user-secrets set "Jwt:Audience" "CaptainCrave.Client"

dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Server=localhost;Database=CaptainCraveDb;Trusted_Connection=True;TrustServerCertificate=True"
```

Adjust the connection string's `Server=` value to match your SQL Server instance (e.g. `(localdb)\MSSQLLocalDB` if you have LocalDB installed instead of a full instance).

### 2. Apply database migrations

```bash
dotnet ef database update
```

This creates the `CaptainCraveDb` database and applies all migrations (users, restaurants, menus, categories, menu items, orders).

### 3. Run the API

Standalone:

```bash
dotnet run
```

Or via the Aspire AppHost (also starts the Angular client), from `Backend/CaptainCrave.AppHost`:

```bash
aspire run
```

The API listens on the ports configured in `Properties/launchSettings.json`. In Development, Swagger UI is available at `/swagger`.

## API overview

| Area | Base route | Notes |
|------|-----------|-------|
| Auth | `POST /api/auth/register`, `POST /api/auth/login` | Returns a JWT on success |
| Users | `GET /api/users/me` | Current authenticated user's profile |
| Restaurants | `GET /api/restaurants`, `GET /api/restaurants/{id}`, `GET /api/restaurants/{id}/menus`, `GET /api/restaurants/{id}/menu-items`, `POST /api/restaurants` | `nearby`/`me` variants for location search and the owner's own restaurant |
| Menus | `GET /api/menus/restaurant/{restaurantId}`, `GET /api/menus/{id}`, `POST /api/menus` | A restaurant can have multiple menus |
| Categories | `GET /api/categories/restaurant/{restaurantId}`, `GET /api/categories/menu/{menuId}`, `POST /api/categories` | Optional grouping for menu items within a menu |
| Menu items | `GET /api/menuitems/restaurant/{restaurantId}`, `GET /api/menuitems/menu/{menuId}`, `POST/PUT/DELETE` | `CategoryId` is optional |
| Orders | `POST /api/orders`, `GET /api/orders/{id}`, `GET /api/orders/customer/*`, `GET /api/orders/restaurant/*`, `PATCH /api/orders/{id}/status` | Role-restricted by `Customer`/`Restaurant`/`Admin` |

Most write endpoints require `Authorize(Roles = ...)` and a `Bearer` token obtained from `/api/auth/login`.

## Tests

Unit tests live in `Backend/CaptainCrave.Tests`:

```bash
cd ../CaptainCrave.Tests
dotnet test
```
