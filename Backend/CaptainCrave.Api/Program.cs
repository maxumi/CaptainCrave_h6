using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Api.Data;
using Api.Models;
using Api.Hubs;
using Api.Middleware;
using Api.Repositories;
using Api.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;
using Serilog;

// Bootstrap logger: catches any startup failure before the host's own Serilog config is read from appsettings.
Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .CreateBootstrapLogger();

var builder = WebApplication.CreateBuilder(args);

// Replaces the default .NET logger with Serilog, configured entirely from the "Serilog" section in appsettings.json.
// preserveStaticLogger: true keeps each host's logger independent of the shared static Log.Logger, which
// avoids a "logger is already frozen" crash when tests spin up multiple WebApplicationFactory<Program> hosts.
builder.Host.UseSerilog((context, services, configuration) => configuration
    .ReadFrom.Configuration(context.Configuration)
    .ReadFrom.Services(services)
    .Enrich.FromLogContext(), preserveStaticLogger: true);

// Controllers
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.Converters.Add(
            new JsonStringEnumConverter(JsonNamingPolicy.CamelCase)
        );
    });

// SignalR (live ordre-notifikationer)
builder.Services.AddSignalR();

// Swagger
builder.Services.AddEndpointsApiExplorer();

builder.Services.AddSwaggerGen(options =>
{
    options.AddSecurityDefinition(
        JwtBearerDefaults.AuthenticationScheme,
        new OpenApiSecurityScheme
        {
            Name = "Authorization",
            Description = "Enter: Bearer {token}",
            In = ParameterLocation.Header,
            Type = SecuritySchemeType.Http,
            Scheme = "bearer",
            BearerFormat = "JWT"
        });

    options.AddSecurityRequirement(doc => new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecuritySchemeReference(
                JwtBearerDefaults.AuthenticationScheme,
                doc),
            []
        }
    });
});

// CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowClient", policy =>
        policy.WithOrigins("https://localhost:4200")
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials()); // SignalR kræver credentials sammen med en specifik origin
});

builder.Services
    .AddIdentityCore<User>()
    .AddRoles<IdentityRole<int>>()
    .AddEntityFrameworkStores<AppDbContext>()
    .AddDefaultTokenProviders();

// DI — Auth
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<ITokenService, TokenService>();

// DI — Restaurant / Menu / Category / MenuItem
builder.Services.AddScoped<IRestaurantRepository, RestaurantRepository>();
builder.Services.AddScoped<IMenuRepository, MenuRepository>();
builder.Services.AddScoped<ICategoryRepository, CategoryRepository>();
builder.Services.AddScoped<IMenuItemRepository, MenuItemRepository>();

builder.Services.AddScoped<IRestaurantService, RestaurantService>();
builder.Services.AddScoped<IMenuService, MenuService>();
builder.Services.AddScoped<ICategoryService, CategoryService>();
builder.Services.AddScoped<IMenuItemService, MenuItemService>();

// DI — Image uploads (stored on local disk under wwwroot/uploads)
builder.Services.AddScoped<IImageStorageService, LocalImageStorageService>();

// DI — Orders
builder.Services.AddScoped<IOrderRepository, OrderRepository>();
builder.Services.AddScoped<IOrderService, OrderService>();

// DI — Payments (falsk/mock betalingssystem)
builder.Services.AddScoped<IPaymentRepository, PaymentRepository>();
builder.Services.AddScoped<IPaymentService, PaymentService>();

// DI — Notifikationer (SignalR)
builder.Services.AddScoped<INotificationService, SignalRNotificationService>();

// Global exception handling — catches anything a controller didn't already handle itself,
// logs it via Serilog and returns a consistent ProblemDetails response instead of a raw stack trace.
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddProblemDetails();

// JWT Authentication
var jwtSecret = builder.Configuration["Jwt:Secret"]
    ?? throw new InvalidOperationException("Jwt:Secret is not configured.");

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,

            ValidIssuer = builder.Configuration["Jwt:Issuer"],
            ValidAudience = builder.Configuration["Jwt:Audience"],

            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(jwtSecret))
        };

        // SignalR sender JWT'et via query-string for WebSocket/SSE, ikke Authorization-headeren.
        options.Events = new JwtBearerEvents
        {
            OnMessageReceived = context =>
            {
                var accessToken = context.Request.Query["access_token"];
                if (!string.IsNullOrEmpty(accessToken) &&
                    context.HttpContext.Request.Path.StartsWithSegments("/hubs"))
                {
                    context.Token = accessToken;
                }
                return Task.CompletedTask;
            }
        };
    });

// Database
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("DefaultConnection")));

// Build app
var app = builder.Build();

// Structured request logging (method, path, status code, elapsed time) for every request.
app.UseSerilogRequestLogging();

// Must run before any middleware that could throw, so it can catch exceptions from the rest of the pipeline.
app.UseExceptionHandler();

// Seed testdata (kun i Development), så der ikke skal oprette data manuelt.
if (app.Environment.IsDevelopment())
{
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    var userManager =
        scope.ServiceProvider.GetRequiredService<UserManager<User>>();

    await DbSeeder.SeedAsync(db, userManager);}

// Swagger
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// Redirect HTTP to HTTPS
app.UseHttpsRedirection();

// Serves uploaded images from wwwroot/uploads at /uploads/*
app.UseStaticFiles();

app.UseCors("AllowClient");

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.MapHub<NotificationHub>("/hubs/notifications");

try
{
    app.Run();
}
finally
{
    Log.CloseAndFlush();
}

// Gør Program synlig for CaptainCrave.Tests, så WebApplicationFactory<Program> kan starte API'en i tests.
public partial class Program { }