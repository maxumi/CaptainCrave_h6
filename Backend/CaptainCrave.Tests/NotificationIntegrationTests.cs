using System.Linq;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Api.Data;
using Api.DTOs;
using Api.DTOs.Auth;
using Api.Models.Enums;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Xunit;

namespace CaptainCrave.Tests;

// Starter hele API'en i hukommelsen (rigtig SignalR-hub, rigtig auth) med en isoleret InMemory-database pr. test. 
// , uden at den skal køre som en rigtig server på en port.
public class CaptainCraveApiFactory : WebApplicationFactory<Program> 
{
    // laver et unikt database-navn for hver test-factory, med GUID (128-bit unik identifier).
    private readonly string _databaseName = $"CaptainCraveTests-{Guid.NewGuid()}";

    // Udskifter den rigtige SQL Server med en tom database i hukommelsen, så testen er hurtig,
    // isoleret og ikke ændrer udviklingsdatabasen.
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureServices(services =>
        {
            // Fjern ALLE EF-registreringer for AppDbContext (SQL Server), ellers ender begge
            // providers med at være registreret samtidig, hvilket EF Core ikke tillader.
            services.RemoveAll<DbContextOptions<AppDbContext>>();
            services.RemoveAll<AppDbContext>();
            services.RemoveAll<IDbContextOptionsConfiguration<AppDbContext>>();

            // Registrerer AppDbContext igen, men denne gang mod InMemory-databasen for denne factory.
            services.AddDbContext<AppDbContext>(options =>
                options.UseInMemoryDatabase(_databaseName));
        });
    }
}

// INTEGRATIONSTEST
// End-to-end test der beviser at SignalR-notifikationerne rent faktisk bliver sendt og modtaget.

// Testen bruger rigtige HTTP-kald og en rigtig SignalR-klient, for at bevise at hele kæden
// (controller -> service -> hub -> klient) faktisk virker sammen, ikke bare hver del for sig.

// IClassFixture: opretter kun en gang og genbruges i alle tests i denne klasse
public class NotificationIntegrationTests : IClassFixture<CaptainCraveApiFactory>
{
    private readonly CaptainCraveApiFactory _factory;

    // Gemmer factoryen, der starter den rigtige API-pipeline til integrationstesten.
    public NotificationIntegrationTests(CaptainCraveApiFactory factory)
    {
        _factory = factory;
    }

   
    [Fact(Timeout = 30000)] // Testcase timeout: 30 sekunder

    // Tester at både "NewOrder" og "OrderStatusChanged" notifikationer bliver modtaget live.
    public async Task NewOrder_And_OrderStatusChanged_Notifications_Are_Received_Live()
    {
        // Rigtige SignalR-klienter forbindes til API'et i hukommelsen.
        // Testen beviser eventnavn, gruppevalg og at beskeden faktisk bliver modtaget.
        using var client = _factory.CreateClient();

        // 1. Opret en restaurant-bruger og en kunde-bruger, og log dem ind.
        var (_, restaurantToken) = await RegisterAndLoginAsync(client, "restaurant@test.dk", UserRole.Restaurant);
        var (customerUserId, customerToken) = await RegisterAndLoginAsync(client, "customer@test.dk", UserRole.Customer);

        // 2. Byg restauranten op: restaurant -> menu -> menuitem, som kunden bagefter bestiller fra.
        var restaurantId = await CreateRestaurantAsync(client, restaurantToken);
        var menuId = await CreateMenuAsync(client, restaurantToken, restaurantId);
        var menuItemId = await CreateMenuItemAsync(client, restaurantToken, menuId);

        // 3. Restauranten forbinder til hub'en og lytter efter "NewOrder".
        await using var restaurantConnection = await ConnectAsync(restaurantToken);
        var newOrderReceived = new TaskCompletionSource<int>();
        restaurantConnection.On<object>("NewOrder", payload =>
        {
            var orderId = ((System.Text.Json.JsonElement)payload).GetProperty("orderId").GetInt32();
            newOrderReceived.TrySetResult(orderId);
        });

        // 4. Kunden afgiver en ordre, den oprettes med status AwaitingPayment.
        var orderId = await CreateOrderAsync(client, customerToken, customerUserId, restaurantId, menuItemId);

        // 4b. Kunden gennemfører den falske betaling, dette bør udløse "NewOrder" til restauranten.
        await CompletePaymentAsync(client, customerToken, orderId);

        var receivedNewOrderId = await WaitWithTimeoutAsync(newOrderReceived.Task);
        Assert.Equal(orderId, receivedNewOrderId);

        // 5. Kunden forbinder til hub'en og lytter efter "OrderStatusChanged".
        await using var customerConnection = await ConnectAsync(customerToken);
        var statusChangedReceived = new TaskCompletionSource<(int OrderId, string Status)>();
        customerConnection.On<object>("OrderStatusChanged", payload =>
        {
            var element = (System.Text.Json.JsonElement)payload;
            statusChangedReceived.TrySetResult((
                element.GetProperty("orderId").GetInt32(),
                element.GetProperty("status").GetString()!));
        });

        // 6. Restauranten opdaterer ordrens status, dette bør udløse "OrderStatusChanged" til kunden.
        await UpdateOrderStatusAsync(client, restaurantToken, orderId, OrderStatus.Preparing);

        var (receivedStatusOrderId, receivedStatus) = await WaitWithTimeoutAsync(statusChangedReceived.Task);
        Assert.Equal(orderId, receivedStatusOrderId);
        Assert.Equal(nameof(OrderStatus.Preparing), receivedStatus);
    }


    // HJÆLPERMETODER: hjælper testcasen 

    // Venter på en besked med en fornuftig timeout, så testen fejler tydeligt i stedet for at hænge.
    private static async Task<T> WaitWithTimeoutAsync<T>(Task<T> task)
    {
        // Venter på enten beskeden eller en 10-sekunders timeout, alt efter hvad der sker først.
        var completed = await Task.WhenAny(task, Task.Delay(TimeSpan.FromSeconds(10)));
        // Fejler tydeligt, hvis det var timeout'en, der vandt (beskeden kom aldrig).
        Assert.True(completed == task, "Notifikationen blev aldrig modtaget inden for tidsgrænsen.");
        // Henter selve resultatet fra den nu færdige besked-task.
        return await task;
    }

    // Registrerer en ny bruger med den ønskede rolle og logger straks ind for at få et JWT.
    private static async Task<(int UserId, string Token)> RegisterAndLoginAsync(HttpClient client, string email, UserRole role)
    {
        // Bygger request-body'en til registrerings-endpointet med den ønskede rolle.
        var register = new RegisterRequestDto
        {
            Name = "Test Bruger",
            Email = email,
            Password = "P@ssword123",
            Address = "Testvej 1",
            Role = role
        };

        // Sender selve registrerings-kaldet til det rigtige auth-endpoint.
        var response = await client.PostAsJsonAsync("/api/auth/register", register);
        // Stopper testen tydeligt her, hvis registreringen fejlede.
        await EnsureSuccessAsync(response);

        // Læser JWT-tokenet og bruger-id'et ud af svaret, så kaldende kode kan autentificere sig videre.
        var auth = await response.Content.ReadFromJsonAsync<AuthResponseDto>(JsonOptions);
        return (auth!.Id, auth.Token);
    }

    // Opretter en restaurant for den nuværende (autentificerede) restaurant-bruger.
    private static async Task<int> CreateRestaurantAsync(HttpClient client, string token)
    {
        // Bygger request-body'en med testdata til en ny restaurant.
        var dto = new CreateRestaurantDto
        {
            Name = "Captain Crave Test",
            Address = "Havnegade 1",
            Latitude = 55.6761,
            Longitude = 12.5683
        };

        // Sender det autentificerede POST-kald for at oprette restauranten.
        var response = await PostAsJsonWithAuthAsync(client, "/api/restaurants", dto, token);
        await EnsureSuccessAsync(response);

        // Henter det oprettede restaurant-id ud af svaret.
        var restaurant = await response.Content.ReadFromJsonAsync<RestaurantDto>(JsonOptions);
        return restaurant!.Id;
    }

    // Opretter en menu under den givne restaurant.
    private static async Task<int> CreateMenuAsync(HttpClient client, string token, int restaurantId)
    {
        // Bygger request-body'en til en ny menu under den givne restaurant.
        var dto = new CreateMenuDto { RestaurantId = restaurantId, Name = "Frokostmenu" };
        // Sender det autentificerede POST-kald for at oprette menuen.
        var response = await PostAsJsonWithAuthAsync(client, "/api/menus", dto, token);
        await EnsureSuccessAsync(response);

        // Henter det oprettede menu-id ud af svaret.
        var menu = await response.Content.ReadFromJsonAsync<MenuDto>(JsonOptions);
        return menu!.Id;
    }

    // Opretter en menuvare under den givne menu (uden kategori, da kategori er valgfri).
    private static async Task<int> CreateMenuItemAsync(HttpClient client, string token, int menuId)
    {
        // Bygger request-body'en med testdata til en ny menuvare.
        var dto = new CreateMenuItemDto
        {
            MenuId = menuId,
            Name = "Bøf Burger",
            Description = "Med pommes frites",
            Price = 89.00m,
            IsAvailable = true
        };

        // Sender det autentificerede POST-kald for at oprette menuvaren.
        var response = await PostAsJsonWithAuthAsync(client, "/api/menuitems", dto, token);
        await EnsureSuccessAsync(response);

        // Henter det oprettede menuvare-id ud af svaret.
        var menuItem = await response.Content.ReadFromJsonAsync<MenuItemDto>(JsonOptions);
        return menuItem!.Id;
    }

    // Afgiver en ordre som kunden, med ét eksemplar af den givne menuvare, til afhentning.
    private static async Task<int> CreateOrderAsync(HttpClient client, string token, int userId, int restaurantId, int menuItemId)
    {
        // Bygger request-body'en: én ordre med ét eksemplar af menuvaren, til afhentning.
        var dto = new CreateOrderDto
        {
            UserId = userId,
            RestaurantId = restaurantId,
            DeliveryType = DeliveryType.Pickup,
            Items = [new CreateOrderItemDto { MenuItemId = menuItemId, Quantity = 1 }]
        };

        // Sender det autentificerede POST-kald for at oprette ordren.
        var response = await PostAsJsonWithAuthAsync(client, "/api/orders", dto, token);
        await EnsureSuccessAsync(response);

        // Henter den oprettede ordres id ud af svaret.
        var order = await response.Content.ReadFromJsonAsync<OrderDto>(JsonOptions);
        return order!.Id;
    }

    // Opdaterer en ordres status som restauranten.
    private static async Task UpdateOrderStatusAsync(HttpClient client, string token, int orderId, OrderStatus status)
    {
        // Bygger request-body'en med den nye ønskede status.
        var dto = new UpdateOrderStatusDto { Status = status };

        // PATCH bruges, fordi kaldet kun ændrer ét felt (status) på en eksisterende ordre.
        var request = new HttpRequestMessage(HttpMethod.Patch, $"/api/orders/{orderId}/status")
        {
            Content = JsonContent.Create(dto)
        };
        // Tilføjer JWT'et, så kaldet bliver godkendt som den korrekte bruger.
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Sender selve requesten og stopper testen tydeligt, hvis den fejler.
        var response = await client.SendAsync(request);
        await EnsureSuccessAsync(response);
    }

    // Gennemfører en falsk betaling for den givne ordre som kunden (kortnummer der ikke slutter på "0000" godkendes altid).
    private static async Task CompletePaymentAsync(HttpClient client, string token, int orderId)
    {
        // Bygger request-body'en med et kortnummer, som den falske gateway altid godkender.
        var dto = new CreatePaymentDto { OrderId = orderId, CardNumber = "4111111111111111" };
        // Sender det autentificerede POST-kald, som bør udløse "NewOrder"-notifikationen.
        var response = await PostAsJsonWithAuthAsync(client, "/api/payments", dto, token);
        await EnsureSuccessAsync(response);
    }

    // Fælles JSON-indstillinger så camelCase-svar (inkl. enum-strenge) fra API'et matcher vores DTO'er.
    private static readonly System.Text.Json.JsonSerializerOptions JsonOptions = new(System.Text.Json.JsonSerializerDefaults.Web)
    {
        Converters = { new System.Text.Json.Serialization.JsonStringEnumConverter(System.Text.Json.JsonNamingPolicy.CamelCase) }
    };

    // Tjekker om et HTTP-svar var en succes, og medtager response-body i fejlbeskeden hvis ikke.
    private static async Task EnsureSuccessAsync(HttpResponseMessage response)
    {
        // Går videre uden mere arbejde, hvis kaldet lykkedes.
        if (response.IsSuccessStatusCode)
            return;

        // Læser response-body'en med, så fejlbeskeden viser årsagen (fx valideringsfejl fra API'et).
        var body = await response.Content.ReadAsStringAsync();
        throw new InvalidOperationException($"{response.RequestMessage?.Method} {response.RequestMessage?.RequestUri} failed with {response.StatusCode}: {body}");
    }

    // Sender et autentificeret POST-kald med et JWT i Authorization-headeren.
    private static Task<HttpResponseMessage> PostAsJsonWithAuthAsync<T>(HttpClient client, string url, T body, string token)
    {
        // Bygger en POST-request med body'en serialiseret som JSON.
        var request = new HttpRequestMessage(HttpMethod.Post, url) { Content = JsonContent.Create(body) };
        // Tilføjer JWT'et, så kaldet bliver godkendt som den korrekte bruger.
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        // Sender requesten videre til kalderen, som selv håndterer svaret.
        return client.SendAsync(request);
    }

    // Opretter og starter en autentificeret SignalR-forbindelse til notifikations-hub'en.
    // LongPolling bruges fordi TestServer'en ikke understøtter rigtige WebSockets.
    private async Task<HubConnection> ConnectAsync(string token)
    {
        var connection = new HubConnectionBuilder()
            .WithUrl(_factory.Server.BaseAddress + "hubs/notifications", options =>
            {
                // Sender requests direkte ind i TestServer'en i hukommelsen, uden en rigtig netværksforbindelse.
                options.HttpMessageHandlerFactory = _ => _factory.Server.CreateHandler();
                // TestServer'en understøtter ikke rigtige WebSockets, så LongPolling bruges i stedet.
                options.Transports = Microsoft.AspNetCore.Http.Connections.HttpTransportType.LongPolling;
                // Leverer JWT'et, så [Authorize] på hubben lader forbindelsen igennem.
                options.AccessTokenProvider = () => Task.FromResult<string?>(token);
            })
            .Build();

        // Starter selve SignalR-forbindelsen (svarer til klientens "login til chatrummet").
        await connection.StartAsync();
        return connection;
    }
}
