using System.Net.Http.Headers;
using System.Net.Http.Json;
using Api.Data;
using Api.DTOs;
using Api.DTOs.Auth;
using Api.Models.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace CaptainCrave.Tests;

// End-to-end tests, der beviser at soft delete-pipelinen rent faktisk virker mod en rigtig database
// (rigtige EF Core query filters, rigtig autentificering, rigtige ejerskabstjek) frem for mockede services.
// Genbruger CaptainCraveApiFactory (en isoleret InMemory-database pr. factory-instans) fra
// NotificationIntegrationTests.cs.
public class SoftDeleteIntegrationTests : IClassFixture<CaptainCraveApiFactory>
{
    private readonly CaptainCraveApiFactory _factory;

    // Gemmer den test-fabrik, der starter API'en og stiller den isolerede testdatabase til rådighed.
    public SoftDeleteIntegrationTests(CaptainCraveApiFactory factory)
    {
        _factory = factory;
    }

    // En ret kan slettes, ses i papirkurven, gendannes og til sidst slettes for altid, hele livscyklussen virker.
    [Fact]
    public async Task MenuItem_SoftDelete_Restore_And_HardDelete_FullLifecycle()
    {
        using var client = _factory.CreateClient();

        var (_, token) = await RegisterAndLoginAsync(client, "owner1@test.dk");
        var restaurantId = await CreateRestaurantAsync(client, token, "Test Diner");
        var menuId = await CreateMenuAsync(client, token, restaurantId);
        var menuItemId = await CreateMenuItemAsync(client, token, menuId, "Burger");

        // Soft delete: retten skal forsvinde fra den normale liste.
        await DeleteAsync(client, token, $"/api/menuitems/{menuItemId}");
        var afterDelete = await GetJsonAsync<List<MenuItemDto>>(client, token, $"/api/menuitems/menu/{menuId}");
        Assert.DoesNotContain(afterDelete!, i => i.Id == menuItemId);

        // Den skal vises i papirkurv-listen.
        var trash = await GetJsonAsync<List<MenuItemDto>>(client, token, $"/api/menuitems/restaurant/{restaurantId}/deleted");
        Assert.Contains(trash!, i => i.Id == menuItemId && i.IsDeleted);

        // Gendan: retten skal dukke op igen i den normale liste.
        await PostAsync(client, token, $"/api/menuitems/{menuItemId}/restore");
        var afterRestore = await GetJsonAsync<List<MenuItemDto>>(client, token, $"/api/menuitems/menu/{menuId}");
        Assert.Contains(afterRestore!, i => i.Id == menuItemId);

        // Hard delete: rækken skal være væk permanent, også selvom man ignorerer query filters.
        await DeleteAsync(client, token, $"/api/menuitems/{menuItemId}/permanent");

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var stillExists = await db.MenuItems.IgnoreQueryFilters().AnyAsync(i => i.Id == menuItemId);
        Assert.False(stillExists);
    }

    // Sletter man en menu, forsvinder dens kategorier og retter også, og de kommer tilbage, når menuen gendannes.
    [Fact]
    public async Task Menu_SoftDelete_HidesItsCategoriesAndMenuItemsToo()
    {
        using var client = _factory.CreateClient();

        var (_, token) = await RegisterAndLoginAsync(client, "owner2@test.dk");
        var restaurantId = await CreateRestaurantAsync(client, token, "Cascade Diner");
        var menuId = await CreateMenuAsync(client, token, restaurantId);
        var categoryId = await CreateCategoryAsync(client, token, menuId, "Drinks");
        var menuItemId = await CreateMenuItemAsync(client, token, menuId, "Cola");

        // Soft delete menuen selv (ikke kategorien eller retten direkte).
        await DeleteAsync(client, token, $"/api/menus/{menuId}");

        // Query filteret skal kaskadere: kategorier og retter under denne menu bliver også skjult,
        // uden at de selv skal soft-deletes enkeltvist.
        var categories = await GetJsonAsync<List<CategoryDto>>(client, token, $"/api/categories/menu/{menuId}");
        Assert.Empty(categories!);

        var items = await GetJsonAsync<List<MenuItemDto>>(client, token, $"/api/menuitems/menu/{menuId}");
        Assert.Empty(items!);

        // Gendannelse af menuen skal bringe dens retter tilbage (retterne blev kaskade-soft-deleted sammen med den).
        await PostAsync(client, token, $"/api/menus/{menuId}/restore");
        var itemsAfterRestore = await GetJsonAsync<List<MenuItemDto>>(client, token, $"/api/menuitems/menu/{menuId}");
        Assert.Contains(itemsAfterRestore!, i => i.Id == menuItemId);

        // Sanity-tjek at kategori-id'et faktisk blev oprettet og er en rigtig række (ubrugt variabel ville være mærkeligt).
        Assert.True(categoryId > 0);
    }

    // Fælles JSON-indstillinger, så camelCase-svar (inklusive enum-strenge) matcher vores DTO'er.
    private static readonly System.Text.Json.JsonSerializerOptions JsonOptions = new(System.Text.Json.JsonSerializerDefaults.Web)
    {
        Converters = { new System.Text.Json.Serialization.JsonStringEnumConverter(System.Text.Json.JsonNamingPolicy.CamelCase) }
    };

    /// <summary>
    /// Opretter en restaurantejer og returnerer både brugerens id og det JWT, som de næste API-kald skal bruge.
    /// </summary>
    /// <returns>Brugerens id og et gyldigt login-token.</returns>
    private static async Task<(int UserId, string Token)> RegisterAndLoginAsync(HttpClient client, string email)
    {
        var register = new RegisterRequestDto
        {
            Name = "Test Owner",
            Email = email,
            Password = "P@ssword123",
            Address = "Testvej 1",
            Role = UserRole.Restaurant
        };

        var response = await client.PostAsJsonAsync("/api/auth/register", register);
        await EnsureSuccessAsync(response);

        var auth = await response.Content.ReadFromJsonAsync<AuthResponseDto>(JsonOptions);
        return (auth!.Id, auth.Token);
    }

    /// <summary>Opretter en restaurant gennem det rigtige API.</summary>
    /// <returns>Id'et på den restaurant, API'et oprettede.</returns>
    private static async Task<int> CreateRestaurantAsync(HttpClient client, string token, string name)
    {
        var dto = new CreateRestaurantDto { Name = name, Address = "Havnegade 1", Latitude = 55.6761, Longitude = 12.5683 };
        var response = await PostAsJsonWithAuthAsync(client, "/api/restaurants", dto, token);
        await EnsureSuccessAsync(response);

        var restaurant = await response.Content.ReadFromJsonAsync<RestaurantDto>(JsonOptions);
        return restaurant!.Id;
    }

    /// <summary>Opretter en menu under en bestemt restaurant gennem API'et.</summary>
    /// <returns>Id'et på den nye menu.</returns>
    private static async Task<int> CreateMenuAsync(HttpClient client, string token, int restaurantId)
    {
        var dto = new CreateMenuDto { RestaurantId = restaurantId, Name = "Menukort" };
        var response = await PostAsJsonWithAuthAsync(client, "/api/menus", dto, token);
        await EnsureSuccessAsync(response);

        var menu = await response.Content.ReadFromJsonAsync<MenuDto>(JsonOptions);
        return menu!.Id;
    }

    /// <summary>Opretter en kategori i en bestemt menu gennem API'et.</summary>
    /// <returns>Id'et på den nye kategori.</returns>
    private static async Task<int> CreateCategoryAsync(HttpClient client, string token, int menuId, string name)
    {
        var dto = new CreateCategoryDto { MenuId = menuId, Name = name };
        var response = await PostAsJsonWithAuthAsync(client, "/api/categories", dto, token);
        await EnsureSuccessAsync(response);

        var category = await response.Content.ReadFromJsonAsync<CategoryDto>(JsonOptions);
        return category!.Id;
    }

    /// <summary>Opretter en ret i en bestemt menu gennem API'et.</summary>
    /// <returns>Id'et på den nye ret.</returns>
    private static async Task<int> CreateMenuItemAsync(HttpClient client, string token, int menuId, string name)
    {
        var dto = new CreateMenuItemDto { MenuId = menuId, Name = name, Description = "Test item", Price = 25.00m, IsAvailable = true };
        var response = await PostAsJsonWithAuthAsync(client, "/api/menuitems", dto, token);
        await EnsureSuccessAsync(response);

        var menuItem = await response.Content.ReadFromJsonAsync<MenuItemDto>(JsonOptions);
        return menuItem!.Id;
    }

    /// <summary>Sender et godkendt GET-kald og læser JSON-svaret som den ønskede type.</summary>
    /// <returns>Det læste svar, eller null hvis svaret ikke indeholder data.</returns>
    private static async Task<T?> GetJsonAsync<T>(HttpClient client, string token, string url)
    {
        var request = new HttpRequestMessage(HttpMethod.Get, url);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await client.SendAsync(request);
        await EnsureSuccessAsync(response);

        return await response.Content.ReadFromJsonAsync<T>(JsonOptions);
    }

    // Sender et godkendt DELETE-kald og sikrer, at serveren accepterede det.
    private static async Task DeleteAsync(HttpClient client, string token, string url)
    {
        var request = new HttpRequestMessage(HttpMethod.Delete, url);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await client.SendAsync(request);
        await EnsureSuccessAsync(response);
    }

    // Sender et godkendt POST-kald uden body, for eksempel når noget skal gendannes.
    private static async Task PostAsync(HttpClient client, string token, string url)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, url);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await client.SendAsync(request);
        await EnsureSuccessAsync(response);
    }

    /// <summary>Bygger og sender et godkendt POST-kald med en JSON-body.</summary>
    /// <returns>Serverens HTTP-svar, så testen kan undersøge det.</returns>
    private static Task<HttpResponseMessage> PostAsJsonWithAuthAsync<T>(HttpClient client, string url, T body, string token)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, url) { Content = JsonContent.Create(body) };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return client.SendAsync(request);
    }

    // Stopper testen med en tydelig fejlbesked, hvis et HTTP-kald ikke gav en succes-status.
    private static async Task EnsureSuccessAsync(HttpResponseMessage response)
    {
        if (response.IsSuccessStatusCode)
            return;

        var body = await response.Content.ReadAsStringAsync();
        throw new InvalidOperationException($"{response.RequestMessage?.Method} {response.RequestMessage?.RequestUri} failed with {response.StatusCode}: {body}");
    }
}
