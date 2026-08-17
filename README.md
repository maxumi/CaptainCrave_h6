# CaptainCrave_h6

## CaptainCrave – Setup Guide

Install the newest version of **Node.js** from the official installer.

> Do not install Node.js through Chocolatey.

Update NPM:

```bash
npm install -g npm@latest
```

Install Angular CLI:

```bash
npm install -g @angular/cli@latest
```

Verify Angular:

```bash
ng --version
```

Use **Angular 22**.

Install .NET Aspire:

```bash
winget install Microsoft.Aspire
```

Also make sure **Docker Desktop** is installed and running.

### 2. Configure User Secrets

Run these commands inside the API project:

```bash
dotnet user-secrets init

dotnet user-secrets set "Jwt:Secret" "your-super-secret-key-at-least-32-characters-long"

dotnet user-secrets set "Jwt:Issuer" "CaptainCrave.Api"

dotnet user-secrets set "Jwt:Audience" "CaptainCrave.Client"

dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Server=localhost;Database=CaptainCraveDb;Trusted_Connection=True;TrustServerCertificate=True"
```

Replace the JWT secret with your own secure secret, and adjust the connection string to match your local SQL Server instance.

See [Backend/CaptainCrave.Api/README.md](Backend/CaptainCrave.Api/README.md) for the full API setup guide, project structure, and endpoint overview.
