using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Xunit;

namespace MealBook.Api.IntegrationTests;

public sealed class MealBookApiTests(MealBookApiFixture fixture)
    : IClassFixture<MealBookApiFixture>
{
    [Fact]
    public async Task Startup_AppliesMigrations_AndHealthEndpointConnectsToDatabase()
    {
        using var client = fixture.Factory.CreateClient();

        using var response = await client.GetAsync("/api/health");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        using var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.Equal("ok", body.RootElement.GetProperty("status").GetString());
    }

    [Fact]
    public async Task FrontendAssets_AreServedAndDeepLinksFallBackToIndex()
    {
        using var client = fixture.Factory.CreateClient();

        using var asset = await client.GetAsync("/main-test.js");
        Assert.Equal(HttpStatusCode.OK, asset.StatusCode);
        Assert.Equal(
            "export const integrationAsset = true;",
            (await asset.Content.ReadAsStringAsync()).Trim());
        Assert.StartsWith(
            "text/javascript",
            asset.Content.Headers.ContentType?.MediaType,
            StringComparison.Ordinal);

        using var deepLink = await client.GetAsync("/recipes/test-recipe");
        Assert.Equal(HttpStatusCode.OK, deepLink.StatusCode);
        Assert.Contains(
            "Meal Book test frontend",
            await deepLink.Content.ReadAsStringAsync(),
            StringComparison.Ordinal);
    }

    [Fact]
    public async Task Registration_CreatesStandardUser_AndDeniesAdminOperations()
    {
        using var client = fixture.Factory.CreateClient();
        var email = $"user-{Guid.NewGuid():N}@mealbook.test";
        const string password = "UserPassword123";

        using var registration = await client.PostAsJsonAsync("/api/auth/register", new
        {
            email,
            password
        });
        Assert.Equal(HttpStatusCode.OK, registration.StatusCode);

        using var login = await client.PostAsJsonAsync(
            "/api/identity/login?useCookies=false",
            new { email, password });
        Assert.Equal(HttpStatusCode.OK, login.StatusCode);
        using var loginBody = JsonDocument.Parse(await login.Content.ReadAsStringAsync());
        var token = loginBody.RootElement.GetProperty("accessToken").GetString();
        Assert.False(string.IsNullOrWhiteSpace(token));
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        using var currentUser = await client.GetAsync("/api/auth/me");
        Assert.Equal(HttpStatusCode.OK, currentUser.StatusCode);
        using var userBody = JsonDocument.Parse(await currentUser.Content.ReadAsStringAsync());
        Assert.False(userBody.RootElement.GetProperty("isAdmin").GetBoolean());

        using var createRecipe = await client.PostAsJsonAsync("/api/recipes", ValidRecipe());
        Assert.Equal(HttpStatusCode.Forbidden, createRecipe.StatusCode);
    }

    [Fact]
    public async Task Admin_CanCreateRecipe_WithIngredientSections()
    {
        using var client = fixture.Factory.CreateClient();
        await SignInAdminAsync(client);

        using var create = await client.PostAsJsonAsync("/api/recipes", ValidRecipe());
        Assert.Equal(HttpStatusCode.Created, create.StatusCode);
        using var createdBody = JsonDocument.Parse(await create.Content.ReadAsStringAsync());
        var recipeId = createdBody.RootElement.GetProperty("id").GetGuid();
        Assert.True(createdBody.RootElement.GetProperty("isPublished").GetBoolean());
        Assert.Equal(
            "Sauce",
            createdBody.RootElement.GetProperty("ingredients")[0].GetProperty("section").GetString());

        using var update = await client.PutAsJsonAsync($"/api/recipes/{recipeId}", new
        {
            name = "Integration test recipe",
            servings = 2,
            prepTimeMinutes = 10,
            cookTimeMinutes = 15,
            mealType = "Dinner",
            course = "Main",
            cookingMethod = "Bake",
            cuisine = "Test",
            isPublished = false,
            tags = Array.Empty<string>(),
            ingredients = new[]
            {
                new { name = "Tomato", quantity = 2m, unit = "pieces", section = "Sauce" }
            },
            steps = new[] { "Cook the sauce." },
            equipment = Array.Empty<string>()
        });
        Assert.Equal(HttpStatusCode.OK, update.StatusCode);
        using var updatedBody = JsonDocument.Parse(await update.Content.ReadAsStringAsync());
        Assert.False(updatedBody.RootElement.GetProperty("isPublished").GetBoolean());

        using var anonymousClient = fixture.Factory.CreateClient();
        using var hiddenDetail = await anonymousClient.GetAsync($"/api/recipes/{recipeId}");
        Assert.Equal(HttpStatusCode.NotFound, hiddenDetail.StatusCode);
    }

    [Fact]
    public async Task RecipeImport_RequiresKey_AndCreatesUnpublishedDraft()
    {
        using var client = fixture.Factory.CreateClient();
        var request = ImportRecipe();

        using var unauthorized = await client.PostAsJsonAsync("/api/recipe-imports", request);
        Assert.Equal(HttpStatusCode.Unauthorized, unauthorized.StatusCode);

        client.DefaultRequestHeaders.Add("X-Recipe-Import-Key", new string('k', 48));
        using var imported = await client.PostAsJsonAsync("/api/recipe-imports", request);
        Assert.Equal(HttpStatusCode.Created, imported.StatusCode);
        using var importedBody = JsonDocument.Parse(await imported.Content.ReadAsStringAsync());
        var recipeId = importedBody.RootElement.GetProperty("id").GetGuid();
        Assert.False(importedBody.RootElement.GetProperty("isPublished").GetBoolean());

        client.DefaultRequestHeaders.Remove("X-Recipe-Import-Key");
        using var publicList = await client.GetAsync("/api/recipes");
        Assert.DoesNotContain(
            "Imported integration recipe",
            await publicList.Content.ReadAsStringAsync(),
            StringComparison.Ordinal);
        using var publicDetail = await client.GetAsync($"/api/recipes/{recipeId}");
        Assert.Equal(HttpStatusCode.NotFound, publicDetail.StatusCode);

        await SignInAdminAsync(client);
        using var drafts = await client.GetAsync("/api/recipes/admin/drafts");
        Assert.Equal(HttpStatusCode.OK, drafts.StatusCode);
        Assert.Contains(
            "Imported integration recipe",
            await drafts.Content.ReadAsStringAsync(),
            StringComparison.Ordinal);

        using var anonymousClient = fixture.Factory.CreateClient();
        using var publish = await client.PutAsJsonAsync(
            $"/api/recipes/{recipeId}",
            ImportRecipe());
        Assert.Equal(HttpStatusCode.OK, publish.StatusCode);
        using var publishedBody = JsonDocument.Parse(await publish.Content.ReadAsStringAsync());
        Assert.True(publishedBody.RootElement.GetProperty("isPublished").GetBoolean());

        using var visibleDetail = await anonymousClient.GetAsync($"/api/recipes/{recipeId}");
        Assert.Equal(HttpStatusCode.OK, visibleDetail.StatusCode);
    }

    [Fact]
    public async Task PublishEndpoint_RequiresAdmin_AndMakesDraftPublic()
    {
        using var importClient = fixture.Factory.CreateClient();
        importClient.DefaultRequestHeaders.Add("X-Recipe-Import-Key", new string('k', 48));
        using var imported = await importClient.PostAsJsonAsync(
            "/api/recipe-imports",
            ImportRecipe($"Publish endpoint recipe {Guid.NewGuid():N}"));
        Assert.Equal(HttpStatusCode.Created, imported.StatusCode);
        using var importedBody = JsonDocument.Parse(await imported.Content.ReadAsStringAsync());
        var recipeId = importedBody.RootElement.GetProperty("id").GetGuid();

        using var anonymousClient = fixture.Factory.CreateClient();
        using var denied = await anonymousClient.PostAsync($"/api/recipes/{recipeId}/publish", null);
        Assert.Equal(HttpStatusCode.Unauthorized, denied.StatusCode);

        using var adminClient = fixture.Factory.CreateClient();
        await SignInAdminAsync(adminClient);
        using var published = await adminClient.PostAsync($"/api/recipes/{recipeId}/publish", null);
        Assert.Equal(HttpStatusCode.OK, published.StatusCode);
        using var publishedBody = JsonDocument.Parse(await published.Content.ReadAsStringAsync());
        Assert.True(publishedBody.RootElement.GetProperty("isPublished").GetBoolean());

        using var visible = await anonymousClient.GetAsync($"/api/recipes/{recipeId}");
        Assert.Equal(HttpStatusCode.OK, visible.StatusCode);
        using var missing = await adminClient.PostAsync($"/api/recipes/{Guid.NewGuid()}/publish", null);
        Assert.Equal(HttpStatusCode.NotFound, missing.StatusCode);
    }

    [Fact]
    public async Task RecipeEndpoints_RequireAdminForDraftListing()
    {
        using var client = fixture.Factory.CreateClient();

        using var response = await client.GetAsync("/api/recipes/admin/drafts");
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    private static async Task SignInAdminAsync(HttpClient client)
    {
        using var login = await client.PostAsJsonAsync(
            "/api/identity/login?useCookies=false",
            new { email = "admin@mealbook.test", password = "TestAdmin12345" });
        Assert.Equal(HttpStatusCode.OK, login.StatusCode);
        using var body = JsonDocument.Parse(await login.Content.ReadAsStringAsync());
        var token = body.RootElement.GetProperty("accessToken").GetString();
        Assert.False(string.IsNullOrWhiteSpace(token));
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
    }

    private static object ValidRecipe() => new
    {
        name = $"Integration recipe {Guid.NewGuid():N}",
        description = "Created by an API integration test.",
        servings = 4,
        prepTimeMinutes = 10,
        cookTimeMinutes = 20,
        mealType = "Dinner",
        course = "Main",
        cookingMethod = "Bake",
        cuisine = "Test",
        tags = new[] { "integration" },
        ingredients = new[]
        {
            new { name = "Tomato", quantity = 2m, unit = "pieces", section = "Sauce" }
        },
        steps = new[] { "Prepare ingredients.", "Cook until ready." },
        equipment = new[] { "Pan" }
    };

    private static object ImportRecipe(string name = "Imported integration recipe") => new
    {
        name,
        description = "Imported draft.",
        sourceUrl = "https://example.com/recipe",
        servings = 2,
        prepTimeMinutes = 5,
        cookTimeMinutes = 10,
        mealType = "Dinner",
        course = "Main",
        cookingMethod = "Stovetop",
        cuisine = "Test",
        tags = Array.Empty<string>(),
        ingredients = new[]
        {
            new { name = "Onion", quantity = 1m, unit = "piece", section = "Base" }
        },
        steps = new[] { "Cook the onion." },
        equipment = Array.Empty<string>()
    };
}
