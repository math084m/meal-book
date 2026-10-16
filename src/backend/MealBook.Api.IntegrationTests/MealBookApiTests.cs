using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.SignalR.Client;
using Xunit;

namespace MealBook.Api.IntegrationTests;

public sealed class MealBookApiTests(MealBookApiFixture fixture)
    : IClassFixture<MealBookApiFixture>
{
    private readonly Guid tomatoId = LoadIngredientId(fixture, "Tomato");
    private readonly Guid onionId = LoadIngredientId(fixture, "Onion");

    private static Guid LoadIngredientId(MealBookApiFixture fixture, string nameEn)
    {
        using var client = fixture.Factory.CreateClient();
        using var response = client.GetAsync("/api/ingredients").GetAwaiter().GetResult();
        using var body = JsonDocument.Parse(response.Content.ReadAsStringAsync().GetAwaiter().GetResult());
        return body.RootElement.EnumerateArray()
            .First(item => item.GetProperty("nameEn").GetString() == nameEn)
            .GetProperty("id").GetGuid();
    }

    [Fact]
    public async Task Catalog_ListsTermsAndIngredients_AndRejectsUnknownReferences()
    {
        using var admin = fixture.Factory.CreateClient();
        await SignInAdminAsync(admin);

        var terms = await ReadJsonAsync(await admin.GetAsync("/api/terms"));
        Assert.Contains(terms.EnumerateArray(), term =>
            term.GetProperty("kind").GetString() == "cuisine" && term.GetProperty("slug").GetString() == "danish");

        using var anonymous = fixture.Factory.CreateClient();
        using var denied = await anonymous.PostAsJsonAsync(
            "/api/ingredients",
            new { nameEn = "Nope", nameDa = "Nej" });
        Assert.Equal(HttpStatusCode.Unauthorized, denied.StatusCode);

        var name = $"Test ingredient {Guid.NewGuid():N}";
        using var created = await admin.PostAsJsonAsync("/api/ingredients", new { nameEn = name, nameDa = "Testing" });
        Assert.Equal(HttpStatusCode.Created, created.StatusCode);
        using var duplicate = await admin.PostAsJsonAsync("/api/ingredients", new { nameEn = name, nameDa = "Testing" });
        Assert.Equal(HttpStatusCode.Conflict, duplicate.StatusCode);
        var id = (await ReadJsonAsync(created)).GetProperty("id").GetGuid();
        using var renamed = await admin.PutAsJsonAsync($"/api/ingredients/{id}", new { nameEn = name, nameDa = "Testet" });
        Assert.Equal(HttpStatusCode.OK, renamed.StatusCode);
        using var deleted = await admin.DeleteAsync($"/api/ingredients/{id}");
        Assert.Equal(HttpStatusCode.NoContent, deleted.StatusCode);

        var unknownTerm = new
        {
            name = "Bad terms",
            servings = 1,
            mealTypes = new[] { "not-a-meal-type" },
            ingredients = new[] { new { ingredientId = tomatoId, quantity = 1m, unit = "g", section = "A" } },
            steps = new[] { "Do." }
        };
        using var badTerm = await admin.PostAsJsonAsync("/api/recipes", unknownTerm);
        Assert.Equal(HttpStatusCode.BadRequest, badTerm.StatusCode);

        var unknownIngredient = new
        {
            name = "Bad ingredient",
            servings = 1,
            ingredients = new[] { new { ingredientId = Guid.NewGuid(), quantity = 1m, unit = "g", section = "A" } },
            steps = new[] { "Do." }
        };
        using var badIngredient = await admin.PostAsJsonAsync("/api/recipes", unknownIngredient);
        Assert.Equal(HttpStatusCode.BadRequest, badIngredient.StatusCode);
    }

    [Fact]
    public async Task Recipes_FilterByTermsIngredientsAndTime_AndSuggestByName()
    {
        using var admin = fixture.Factory.CreateClient();
        await SignInAdminAsync(admin);
        var name = $"Filterable {Guid.NewGuid():N}";
        using var create = await admin.PostAsJsonAsync("/api/recipes", new
        {
            name,
            servings = 2,
            prepTimeMinutes = 20,
            cookTimeMinutes = 40,
            mealTypes = new[] { "lunch" },
            courses = new[] { "salad", "side" },
            cookingMethods = new[] { "grilling" },
            cuisines = new[] { "greek" },
            tags = new[] { "vegan", "quick" },
            ingredients = new[]
            {
                new { ingredientId = tomatoId, quantity = 1m, unit = "pieces", section = "Main" },
                new { ingredientId = onionId, quantity = 1m, unit = "pieces", section = "Main" }
            },
            steps = new[] { "Mix." }
        });
        Assert.Equal(HttpStatusCode.Created, create.StatusCode);

        using var anonymous = fixture.Factory.CreateClient();
        async Task<bool> Matches(string query) =>
            (await anonymous.GetStringAsync($"/api/recipes?{query}")).Contains(name, StringComparison.Ordinal);

        Assert.True(await Matches("mealTypes=lunch&mealTypes=dinner"));
        Assert.True(await Matches("courses=side&cuisines=greek&tags=vegan&cookingMethods=grilling"));
        Assert.False(await Matches("mealTypes=breakfast"));
        Assert.False(await Matches("cuisines=greek&tags=keto"));
        Assert.True(await Matches($"ingredients={tomatoId}&ingredients={onionId}"));
        Assert.False(await Matches($"ingredients={tomatoId}&ingredients={Guid.NewGuid()}"));
        Assert.True(await Matches("maxTotalMinutes=60"));
        Assert.False(await Matches("maxTotalMinutes=30"));
        Assert.False(await Matches("minTotalMinutes=240"));

        var suggestions = await ReadJsonAsync(
            await anonymous.GetAsync($"/api/recipes/suggestions?q={name[..18]}"));
        Assert.Contains(suggestions.EnumerateArray(), item => item.GetProperty("name").GetString() == name);
        var none = await ReadJsonAsync(await anonymous.GetAsync("/api/recipes/suggestions?q="));
        Assert.Equal(0, none.GetArrayLength());
    }

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
            mealTypes = new[] { "dinner" },
            courses = new[] { "main" },
            cookingMethods = new[] { "baking" },
            cuisines = new[] { "italian" },
            isPublished = false,
            tags = Array.Empty<string>(),
            ingredients = new[]
            {
                new { ingredientId = tomatoId, quantity = 2m, unit = "pieces", section = "Sauce" }
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
    public async Task LoginToken_IsAcceptedByAnotherInstanceSharingTheDatabase()
    {
        using var machineA = fixture.CreateSecondInstance();
        using var machineB = fixture.CreateSecondInstance();
        using var clientA = machineA.CreateClient();
        await SignInAdminAsync(clientA);

        using var clientB = machineB.CreateClient();
        clientB.DefaultRequestHeaders.Authorization = clientA.DefaultRequestHeaders.Authorization;
        using var response = await clientB.GetAsync("/api/auth/me");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task PublishedRecipe_IsTranslatedBothWays_AndServedByLanguage()
    {
        using var admin = fixture.Factory.CreateClient();
        await SignInAdminAsync(admin);

        var englishName = $"Translation test {Guid.NewGuid():N}";
        using var createEnglish = await admin.PostAsJsonAsync("/api/recipes", TranslatableRecipe(englishName, "en"));
        Assert.Equal(HttpStatusCode.Created, createEnglish.StatusCode);
        var englishId = (await ReadJsonAsync(createEnglish)).GetProperty("id").GetGuid();

        using var anonymous = fixture.Factory.CreateClient();
        var original = await ReadJsonAsync(await anonymous.GetAsync($"/api/recipes/{englishId}"));
        Assert.Equal(englishName, original.GetProperty("name").GetString());
        Assert.False(original.GetProperty("isTranslated").GetBoolean());

        var danish = await ReadJsonAsync(await anonymous.GetAsync($"/api/recipes/{englishId}?lang=da"));
        Assert.Equal($"[da] {englishName}", danish.GetProperty("name").GetString());
        Assert.True(danish.GetProperty("isTranslated").GetBoolean());
        Assert.Equal("da", danish.GetProperty("displayLanguage").GetString());
        Assert.Equal("en", danish.GetProperty("language").GetString());
        var ingredient = danish.GetProperty("ingredients")[0];
        Assert.Equal("Tomat", ingredient.GetProperty("name").GetString());
        Assert.Equal(2m, ingredient.GetProperty("quantity").GetDecimal());
        Assert.Equal("g", ingredient.GetProperty("unit").GetString());
        Assert.Equal("[da] Prepare ingredients.", danish.GetProperty("steps")[0].GetString());

        var sameLanguage = await ReadJsonAsync(await anonymous.GetAsync($"/api/recipes/{englishId}?lang=en"));
        Assert.False(sameLanguage.GetProperty("isTranslated").GetBoolean());

        using var list = await anonymous.GetAsync("/api/recipes?lang=da");
        Assert.Contains($"[da] {englishName}", await list.Content.ReadAsStringAsync());

        var renamed = englishName + " updated";
        using var update = await admin.PutAsJsonAsync($"/api/recipes/{englishId}", TranslatableRecipe(renamed, "en"));
        Assert.Equal(HttpStatusCode.OK, update.StatusCode);
        var afterUpdate = await ReadJsonAsync(await anonymous.GetAsync($"/api/recipes/{englishId}?lang=da"));
        Assert.Equal($"[da] {renamed}", afterUpdate.GetProperty("name").GetString());

        var danishName = $"Oversaettelse {Guid.NewGuid():N}";
        using var createDanish = await admin.PostAsJsonAsync("/api/recipes", TranslatableRecipe(danishName, "da"));
        var danishId = (await ReadJsonAsync(createDanish)).GetProperty("id").GetGuid();
        var english = await ReadJsonAsync(await anonymous.GetAsync($"/api/recipes/{danishId}?lang=en"));
        Assert.Equal($"[en] {danishName}", english.GetProperty("name").GetString());
    }

    [Fact]
    public async Task DraftIsTranslatedOnlyWhenPublished()
    {
        using var admin = fixture.Factory.CreateClient();
        await SignInAdminAsync(admin);
        var name = $"Draft translation {Guid.NewGuid():N}";
        using var import = await ImportAsync(fixture.Factory.CreateClient(), ImportRecipe(name));
        var id = (await ReadJsonAsync(import)).GetProperty("id").GetGuid();

        var draft = await ReadJsonAsync(await admin.GetAsync($"/api/recipes/{id}?lang=da"));
        Assert.False(draft.GetProperty("isTranslated").GetBoolean());

        using var publish = await admin.PostAsync($"/api/recipes/{id}/publish", null);
        Assert.Equal(HttpStatusCode.OK, publish.StatusCode);
        var published = await ReadJsonAsync(await admin.GetAsync($"/api/recipes/{id}?lang=da"));
        Assert.Equal($"[da] {name}", published.GetProperty("name").GetString());
    }

    [Fact]
    public async Task RecipeImage_UploadServeAndDelete_RespectsDraftsAndValidation()
    {
        using var importClient = fixture.Factory.CreateClient();
        using var imported = await ImportAsync(importClient, ImportRecipe($"Image recipe {Guid.NewGuid():N}"));
        var recipeId = (await ReadJsonAsync(imported)).GetProperty("id").GetGuid();
        using var anonymous = fixture.Factory.CreateClient();
        using var admin = fixture.Factory.CreateClient();
        await SignInAdminAsync(admin);

        static MultipartFormDataContent Upload(byte[] bytes)
        {
            var file = new ByteArrayContent(bytes);
            file.Headers.ContentType = new MediaTypeHeaderValue("image/jpeg");
            return new MultipartFormDataContent { { file, "file", "cover.jpg" } };
        }

        using var denied = await anonymous.PostAsync($"/api/recipes/{recipeId}/image", Upload([0xFF, 0xD8, 0xFF, 1]));
        Assert.Equal(HttpStatusCode.Unauthorized, denied.StatusCode);

        using var notImage = await admin.PostAsync($"/api/recipes/{recipeId}/image", Upload([1, 2, 3, 4]));
        Assert.Equal(HttpStatusCode.BadRequest, notImage.StatusCode);

        using var saved = await admin.PostAsync($"/api/recipes/{recipeId}/image", Upload([0xFF, 0xD8, 0xFF, 1, 2]));
        Assert.Equal(HttpStatusCode.OK, saved.StatusCode);

        var recipe = await ReadJsonAsync(await admin.GetAsync($"/api/recipes/{recipeId}"));
        Assert.StartsWith($"recipes/{recipeId}/image?v=", recipe.GetProperty("imageUrl").GetString());

        using var draftHidden = await anonymous.GetAsync($"/api/recipes/{recipeId}/image");
        Assert.Equal(HttpStatusCode.NotFound, draftHidden.StatusCode);
        using var draftAdmin = await admin.GetAsync($"/api/recipes/{recipeId}/image");
        Assert.Equal(HttpStatusCode.OK, draftAdmin.StatusCode);
        Assert.Equal("image/jpeg", draftAdmin.Content.Headers.ContentType?.MediaType);

        (await admin.PostAsync($"/api/recipes/{recipeId}/publish", null)).Dispose();
        using var visible = await anonymous.GetAsync($"/api/recipes/{recipeId}/image");
        Assert.Equal(HttpStatusCode.OK, visible.StatusCode);
        Assert.Equal(5, (await visible.Content.ReadAsByteArrayAsync()).Length);

        using var removed = await admin.DeleteAsync($"/api/recipes/{recipeId}/image");
        Assert.Equal(HttpStatusCode.NoContent, removed.StatusCode);
        using var gone = await anonymous.GetAsync($"/api/recipes/{recipeId}/image");
        Assert.Equal(HttpStatusCode.NotFound, gone.StatusCode);
        var after = await ReadJsonAsync(await admin.GetAsync($"/api/recipes/{recipeId}"));
        Assert.Equal(JsonValueKind.Null, after.GetProperty("imageUrl").ValueKind);
    }

    [Fact]
    public async Task MealPlan_AggregatesShoppingListBySection_TracksChecks_AndSuggestsRecipes()
    {
        using var anonymous = fixture.Factory.CreateClient();
        using var denied = await anonymous.GetAsync("/api/meal-plan");
        Assert.Equal(HttpStatusCode.Unauthorized, denied.StatusCode);

        using var client = fixture.Factory.CreateClient();
        await SignInAdminAsync(client);
        using var cleared = await client.DeleteAsync("/api/meal-plan");
        Assert.Equal(HttpStatusCode.NoContent, cleared.StatusCode);

        var first = await CreatePlanRecipeAsync(client, "Plan A", 2, (tomatoId, 2m, "dl"), (onionId, 1m, "piece"));
        var second = await CreatePlanRecipeAsync(client, "Plan B", 4, (tomatoId, 100m, "ml"));
        var third = await CreatePlanRecipeAsync(client, "Plan C", 2, (tomatoId, 1m, "piece"));
        var draft = await CreatePlanRecipeAsync(client, "Plan draft", 2, (tomatoId, 1m, "piece"), published: false);

        using var addFirst = await client.PutAsJsonAsync($"/api/meal-plan/recipes/{first}", new { });
        Assert.Equal(HttpStatusCode.NoContent, addFirst.StatusCode);
        using var addSecond = await client.PutAsJsonAsync($"/api/meal-plan/recipes/{second}", new { servings = 8 });
        Assert.Equal(HttpStatusCode.NoContent, addSecond.StatusCode);
        using var unknown = await client.PutAsJsonAsync($"/api/meal-plan/recipes/{Guid.NewGuid()}", new { });
        Assert.Equal(HttpStatusCode.NotFound, unknown.StatusCode);
        using var unpublished = await client.PutAsJsonAsync($"/api/meal-plan/recipes/{draft}", new { });
        Assert.Equal(HttpStatusCode.NotFound, unpublished.StatusCode);
        using var badServings = await client.PutAsJsonAsync($"/api/meal-plan/recipes/{second}/servings", new { servings = 0 });
        Assert.Equal(HttpStatusCode.BadRequest, badServings.StatusCode);

        var plan = await ReadJsonAsync(await client.GetAsync("/api/meal-plan?lang=en"));
        Assert.Equal(2, plan.GetProperty("entries").GetArrayLength());
        var produce = plan.GetProperty("sections").EnumerateArray()
            .Single(section => section.GetProperty("slug").GetString() == "produce");
        var tomato = produce.GetProperty("items").EnumerateArray()
            .Single(item => item.GetProperty("name").GetString() == "Tomato");
        // 2 dl for the planned 2 servings plus 100 ml scaled from 4 to 8 servings: 200 + 200 ml.
        Assert.Equal(400m, tomato.GetProperty("quantity").GetDecimal());
        Assert.Equal("ml", tomato.GetProperty("unit").GetString());
        Assert.Equal(2, tomato.GetProperty("recipes").GetArrayLength());
        Assert.False(tomato.GetProperty("checked").GetBoolean());

        using var check = await client.PutAsJsonAsync(
            "/api/meal-plan/checks",
            new { ingredientId = tomatoId, unit = "ml", @checked = true });
        Assert.Equal(HttpStatusCode.NoContent, check.StatusCode);
        var checkedPlan = await ReadJsonAsync(await client.GetAsync("/api/meal-plan"));
        Assert.True(checkedPlan.GetProperty("sections").EnumerateArray()
            .SelectMany(section => section.GetProperty("items").EnumerateArray())
            .Single(item => item.GetProperty("name").GetString() == "Tomato")
            .GetProperty("checked").GetBoolean());

        var suggestions = await ReadJsonAsync(await client.GetAsync("/api/meal-plan/suggestions"));
        var suggestedIds = suggestions.EnumerateArray().Select(item => item.GetProperty("recipeId").GetGuid()).ToList();
        Assert.DoesNotContain(first, suggestedIds);
        Assert.DoesNotContain(second, suggestedIds);
        Assert.DoesNotContain(draft, suggestedIds);
        Assert.All(suggestions.EnumerateArray(), item => Assert.True(item.GetProperty("sharedIngredients").GetArrayLength() > 0));
        Assert.Contains(third, suggestedIds);

        using var removed = await client.DeleteAsync($"/api/meal-plan/recipes/{first}");
        Assert.Equal(HttpStatusCode.NoContent, removed.StatusCode);
        using var removedAgain = await client.DeleteAsync($"/api/meal-plan/recipes/{first}");
        Assert.Equal(HttpStatusCode.NotFound, removedAgain.StatusCode);

        using var emptied = await client.DeleteAsync("/api/meal-plan");
        Assert.Equal(HttpStatusCode.NoContent, emptied.StatusCode);
        var empty = await ReadJsonAsync(await client.GetAsync("/api/meal-plan"));
        Assert.Equal(0, empty.GetProperty("entries").GetArrayLength());
        Assert.Equal(0, empty.GetProperty("sections").GetArrayLength());
    }

    [Fact]
    public async Task MealPlanChanges_ArePushedToConnectedClients_ThroughTheHub()
    {
        using var client = fixture.Factory.CreateClient();
        await SignInAdminAsync(client);
        var token = client.DefaultRequestHeaders.Authorization!.Parameter!;
        var recipeId = await CreatePlanRecipeAsync(client, "Realtime", 2, (tomatoId, 1m, "piece"));

        var server = fixture.Factory.Server;
        await using var anonymousConnection = new Microsoft.AspNetCore.SignalR.Client.HubConnectionBuilder()
            .WithUrl(new Uri(server.BaseAddress, "/api/hubs/mealplan"), options =>
            {
                options.HttpMessageHandlerFactory = _ => server.CreateHandler();
                options.Transports = Microsoft.AspNetCore.Http.Connections.HttpTransportType.LongPolling;
            })
            .Build();
        await Assert.ThrowsAnyAsync<Exception>(() => anonymousConnection.StartAsync());

        var changed = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        await using var connection = new Microsoft.AspNetCore.SignalR.Client.HubConnectionBuilder()
            .WithUrl(new Uri(server.BaseAddress, "/api/hubs/mealplan"), options =>
            {
                options.AccessTokenProvider = () => Task.FromResult<string?>(token);
                options.HttpMessageHandlerFactory = _ => server.CreateHandler();
                options.Transports = Microsoft.AspNetCore.Http.Connections.HttpTransportType.LongPolling;
            })
            .Build();
        connection.On("planChanged", () => changed.TrySetResult());
        await connection.StartAsync();

        using var add = await client.PutAsJsonAsync($"/api/meal-plan/recipes/{recipeId}", new { });
        Assert.Equal(HttpStatusCode.NoContent, add.StatusCode);
        await changed.Task.WaitAsync(TimeSpan.FromSeconds(10));

        using var cleared = await client.DeleteAsync("/api/meal-plan");
        Assert.Equal(HttpStatusCode.NoContent, cleared.StatusCode);
    }

    [Fact]
    public async Task ShopSections_AreListed_AndIngredientsRequireAKnownSection()
    {
        using var client = fixture.Factory.CreateClient();
        await SignInAdminAsync(client);

        var sections = await ReadJsonAsync(await client.GetAsync("/api/shop-sections"));
        Assert.Equal(9, sections.GetArrayLength());
        Assert.Contains(sections.EnumerateArray(), section => section.GetProperty("slug").GetString() == "dairy");

        var name = $"Sectioned {Guid.NewGuid():N}";
        using var bad = await client.PostAsJsonAsync(
            "/api/ingredients",
            new { nameEn = name, nameDa = "Sektion", shopSectionSlug = "nope" });
        Assert.Equal(HttpStatusCode.BadRequest, bad.StatusCode);
        using var created = await client.PostAsJsonAsync(
            "/api/ingredients",
            new { nameEn = name, nameDa = "Sektion", shopSectionSlug = "frozen" });
        Assert.Equal(HttpStatusCode.Created, created.StatusCode);
        Assert.Equal("frozen", (await ReadJsonAsync(created)).GetProperty("shopSectionSlug").GetString());
    }

    [Fact]
    public async Task RecipeEndpoints_RequireAdminForDraftListing()
    {
        using var client = fixture.Factory.CreateClient();

        using var response = await client.GetAsync("/api/recipes/admin/drafts");
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    private static async Task<Guid> CreatePlanRecipeAsync(
        HttpClient client,
        string name,
        int servings,
        params (Guid IngredientId, decimal Quantity, string Unit)[] lines)
        => await CreatePlanRecipeAsync(client, name, servings, true, lines);

    private static async Task<Guid> CreatePlanRecipeAsync(
        HttpClient client,
        string name,
        int servings,
        (Guid IngredientId, decimal Quantity, string Unit) line,
        bool published)
        => await CreatePlanRecipeAsync(client, name, servings, published, [line]);

    private static async Task<Guid> CreatePlanRecipeAsync(
        HttpClient client,
        string name,
        int servings,
        bool published,
        (Guid IngredientId, decimal Quantity, string Unit)[] lines)
    {
        using var response = await client.PostAsJsonAsync("/api/recipes", new
        {
            name = $"{name} {Guid.NewGuid():N}",
            servings,
            isPublished = published,
            ingredients = lines.Select(line => new
            {
                ingredientId = line.IngredientId,
                quantity = line.Quantity,
                unit = line.Unit,
                section = "Main"
            }).ToArray(),
            steps = new[] { "Cook." }
        });
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        return (await ReadJsonAsync(response)).GetProperty("id").GetGuid();
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

    private static async Task<JsonElement> ReadJsonAsync(HttpResponseMessage response)
    {
        using var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        return body.RootElement.Clone();
    }

    private static Task<HttpResponseMessage> ImportAsync(HttpClient client, object payload)
    {
        client.DefaultRequestHeaders.Add("X-Recipe-Import-Key", new string('k', 48));
        return client.PostAsJsonAsync("/api/recipe-imports", payload);
    }

    private object TranslatableRecipe(string name, string language) => new
    {
        name,
        description = "Description text.",
        language,
        servings = 4,
        prepTimeMinutes = 10,
        cookTimeMinutes = 20,
        mealTypes = new[] { "dinner" },
        courses = new[] { "main" },
        cookingMethods = new[] { "baking" },
        cuisines = new[] { "italian" },
        tags = new[] { "quick" },
        ingredients = new[]
        {
            new { ingredientId = tomatoId, quantity = 2m, unit = "g", section = "Sauce" }
        },
        steps = new[] { "Prepare ingredients.", "Cook until ready." },
        equipment = new[] { "Pan" }
    };

    private object ValidRecipe() => new
    {
        name = $"Integration recipe {Guid.NewGuid():N}",
        description = "Created by an API integration test.",
        servings = 4,
        prepTimeMinutes = 10,
        cookTimeMinutes = 20,
        mealTypes = new[] { "dinner" },
        courses = new[] { "main" },
        cookingMethods = new[] { "baking" },
        cuisines = new[] { "italian" },
        tags = new[] { "vegetarian" },
        ingredients = new[]
        {
            new { ingredientId = tomatoId, quantity = 2m, unit = "pieces", section = "Sauce" }
        },
        steps = new[] { "Prepare ingredients.", "Cook until ready." },
        equipment = new[] { "Pan" }
    };

    private object ImportRecipe(string name = "Imported integration recipe") => new
    {
        name,
        description = "Imported draft.",
        sourceUrl = "https://example.com/recipe",
        servings = 2,
        prepTimeMinutes = 5,
        cookTimeMinutes = 10,
        mealTypes = new[] { "dinner" },
        courses = new[] { "main" },
        cookingMethods = new[] { "pan-frying" },
        cuisines = new[] { "danish" },
        tags = Array.Empty<string>(),
        ingredients = new[]
        {
            new { ingredientId = onionId, quantity = 1m, unit = "piece", section = "Base" }
        },
        steps = new[] { "Cook the onion." },
        equipment = Array.Empty<string>()
    };
}
