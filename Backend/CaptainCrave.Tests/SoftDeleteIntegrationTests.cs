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

// End-to-end tests proving the soft delete pipeline actually works against a real database
// (real EF Core query filters, real auth, real ownership checks) rather than mocked services.
// Reuses CaptainCraveApiFactory (an isolated InMemory database per factory instance) from
// NotificationIntegrationTests.cs.
public class SoftDeleteIntegrationTests : IClassFixture<CaptainCraveApiFactory>
{
    private readonly CaptainCraveApiFactory _factory;

    public SoftDeleteIntegrationTests(CaptainCraveApiFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task MenuItem_SoftDelete_Restore_And_HardDelete_FullLifecycle()
    {
        using var client = _factory.CreateClient();

        var (_, token) = await RegisterAndLoginAsync(client, "owner1@test.dk");
        var restaurantId = await CreateRestaurantAsync(client, token, "Test Diner");
        var menuId = await CreateMenuAsync(client, token, restaurantId);
        var menuItemId = await CreateMenuItemAsync(client, token, menuId, "Burger");

        // Soft delete: the item should disappear from the normal listing.
        await DeleteAsync(client, token, $"/api/menuitems/{menuItemId}");
        var afterDelete = await GetJsonAsync<List<MenuItemDto>>(client, token, $"/api/menuitems/menu/{menuId}");
        Assert.DoesNotContain(afterDelete!, i => i.Id == menuItemId);

        // It should show up in the trash listing.
        var trash = await GetJsonAsync<List<MenuItemDto>>(client, token, $"/api/menuitems/restaurant/{restaurantId}/deleted");
        Assert.Contains(trash!, i => i.Id == menuItemId && i.IsDeleted);

        // Restore: the item should reappear in the normal listing.
        await PostAsync(client, token, $"/api/menuitems/{menuItemId}/restore");
        var afterRestore = await GetJsonAsync<List<MenuItemDto>>(client, token, $"/api/menuitems/menu/{menuId}");
        Assert.Contains(afterRestore!, i => i.Id == menuItemId);

        // Hard delete: the row should be gone permanently, even when ignoring query filters.
        await DeleteAsync(client, token, $"/api/menuitems/{menuItemId}/permanent");

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var stillExists = await db.MenuItems.IgnoreQueryFilters().AnyAsync(i => i.Id == menuItemId);
        Assert.False(stillExists);
    }

    [Fact]
    public async Task Menu_SoftDelete_HidesItsCategoriesAndMenuItemsToo()
    {
        using var client = _factory.CreateClient();

        var (_, token) = await RegisterAndLoginAsync(client, "owner2@test.dk");
        var restaurantId = await CreateRestaurantAsync(client, token, "Cascade Diner");
        var menuId = await CreateMenuAsync(client, token, restaurantId);
        var categoryId = await CreateCategoryAsync(client, token, menuId, "Drinks");
        var menuItemId = await CreateMenuItemAsync(client, token, menuId, "Cola");

        // Soft delete the menu itself (not the category or item directly).
        await DeleteAsync(client, token, $"/api/menus/{menuId}");

        // The query filter should cascade: categories and menu items under this menu are hidden too,
        // without needing to soft-delete them individually.
        var categories = await GetJsonAsync<List<CategoryDto>>(client, token, $"/api/categories/menu/{menuId}");
        Assert.Empty(categories!);

        var items = await GetJsonAsync<List<MenuItemDto>>(client, token, $"/api/menuitems/menu/{menuId}");
        Assert.Empty(items!);

        // Restoring the menu should bring its menu items back (menu items were cascade soft-deleted with it).
        await PostAsync(client, token, $"/api/menus/{menuId}/restore");
        var itemsAfterRestore = await GetJsonAsync<List<MenuItemDto>>(client, token, $"/api/menuitems/menu/{menuId}");
        Assert.Contains(itemsAfterRestore!, i => i.Id == menuItemId);

        // Sanity check the category id was actually created and is a real row (unused variable would be a smell).
        Assert.True(categoryId > 0);
    }

    // Shared JSON options so camelCase responses (including enum strings) match our DTOs.
    private static readonly System.Text.Json.JsonSerializerOptions JsonOptions = new(System.Text.Json.JsonSerializerDefaults.Web)
    {
        Converters = { new System.Text.Json.Serialization.JsonStringEnumConverter(System.Text.Json.JsonNamingPolicy.CamelCase) }
    };

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

    private static async Task<int> CreateRestaurantAsync(HttpClient client, string token, string name)
    {
        var dto = new CreateRestaurantDto { Name = name, Address = "Havnegade 1", Latitude = 55.6761, Longitude = 12.5683 };
        var response = await PostAsJsonWithAuthAsync(client, "/api/restaurants", dto, token);
        await EnsureSuccessAsync(response);

        var restaurant = await response.Content.ReadFromJsonAsync<RestaurantDto>(JsonOptions);
        return restaurant!.Id;
    }

    private static async Task<int> CreateMenuAsync(HttpClient client, string token, int restaurantId)
    {
        var dto = new CreateMenuDto { RestaurantId = restaurantId, Name = "Menukort" };
        var response = await PostAsJsonWithAuthAsync(client, "/api/menus", dto, token);
        await EnsureSuccessAsync(response);

        var menu = await response.Content.ReadFromJsonAsync<MenuDto>(JsonOptions);
        return menu!.Id;
    }

    private static async Task<int> CreateCategoryAsync(HttpClient client, string token, int menuId, string name)
    {
        var dto = new CreateCategoryDto { MenuId = menuId, Name = name };
        var response = await PostAsJsonWithAuthAsync(client, "/api/categories", dto, token);
        await EnsureSuccessAsync(response);

        var category = await response.Content.ReadFromJsonAsync<CategoryDto>(JsonOptions);
        return category!.Id;
    }

    private static async Task<int> CreateMenuItemAsync(HttpClient client, string token, int menuId, string name)
    {
        var dto = new CreateMenuItemDto { MenuId = menuId, Name = name, Description = "Test item", Price = 25.00m, IsAvailable = true };
        var response = await PostAsJsonWithAuthAsync(client, "/api/menuitems", dto, token);
        await EnsureSuccessAsync(response);

        var menuItem = await response.Content.ReadFromJsonAsync<MenuItemDto>(JsonOptions);
        return menuItem!.Id;
    }

    private static async Task<T?> GetJsonAsync<T>(HttpClient client, string token, string url)
    {
        var request = new HttpRequestMessage(HttpMethod.Get, url);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await client.SendAsync(request);
        await EnsureSuccessAsync(response);

        return await response.Content.ReadFromJsonAsync<T>(JsonOptions);
    }

    private static async Task DeleteAsync(HttpClient client, string token, string url)
    {
        var request = new HttpRequestMessage(HttpMethod.Delete, url);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await client.SendAsync(request);
        await EnsureSuccessAsync(response);
    }

    private static async Task PostAsync(HttpClient client, string token, string url)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, url);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await client.SendAsync(request);
        await EnsureSuccessAsync(response);
    }

    private static Task<HttpResponseMessage> PostAsJsonWithAuthAsync<T>(HttpClient client, string url, T body, string token)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, url) { Content = JsonContent.Create(body) };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return client.SendAsync(request);
    }

    private static async Task EnsureSuccessAsync(HttpResponseMessage response)
    {
        if (response.IsSuccessStatusCode)
            return;

        var body = await response.Content.ReadAsStringAsync();
        throw new InvalidOperationException($"{response.RequestMessage?.Method} {response.RequestMessage?.RequestUri} failed with {response.StatusCode}: {body}");
    }
}
