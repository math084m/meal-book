using System.Text.Json;
using MealBook.Domain.Recipes;
using Microsoft.Extensions.Logging;

namespace MealBook.Application.Recipes;

public sealed class RecipeService(
    IRecipeRepository repository,
    ICatalogRepository catalog,
    ITranslator translator,
    IImageStorage imageStorage,
    ILogger<RecipeService> logger) : IRecipeService
{
    public async Task<IReadOnlyList<RecipeSuggestionResponse>> GetSuggestionsAsync(
        string? query,
        string? language,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(query))
        {
            return [];
        }

        var recipes = await repository.GetAllAsync(
            new RecipeSearchRequest { Search = query, Sort = "name", Lang = language },
            cancellationToken);
        var map = await CreateMapperAsync(recipes.Take(8).ToArray(), language, cancellationToken);
        return recipes.Take(8)
            .Select(map)
            .Select(recipe => new RecipeSuggestionResponse(recipe.Id, recipe.Name))
            .ToArray();
    }

    public async Task<IReadOnlyList<RecipeResponse>> GetAllAsync(
        RecipeSearchRequest search,
        CancellationToken cancellationToken)
    {
        var recipes = await repository.GetAllAsync(search, cancellationToken);
        var map = await CreateMapperAsync(recipes, search.Lang, cancellationToken);
        return recipes.Select(map).Select(ToSummary).ToArray();
    }

    public async Task<RecipeResponse?> GetByIdAsync(
        Guid id,
        string? language,
        CancellationToken cancellationToken)
    {
        var recipe = await repository.GetByIdAsync(id, cancellationToken);
        if (recipe is null)
        {
            return null;
        }

        var map = await CreateMapperAsync([recipe], language, cancellationToken);
        return map(recipe);
    }

    public async Task<RecipeDashboardResponse> GetDashboardAsync(
        string? language,
        CancellationToken cancellationToken)
    {
        var dashboard = await repository.GetDashboardAsync(cancellationToken);
        var map = await CreateMapperAsync(
            dashboard.RecentlyAdded
                .Concat(dashboard.MostCooked)
                .Concat(dashboard.RecentlyCooked.Select(item => item.Recipe))
                .ToArray(),
            language,
            cancellationToken);
        return new RecipeDashboardResponse(
            dashboard.RecentlyAdded.Select(map).Select(ToSummary).ToArray(),
            dashboard.MostCooked.Select(map).Select(ToSummary).ToArray(),
            dashboard.RecentlyCooked
                .Select(item => new RecentlyCookedRecipeResponse(ToSummary(map(item.Recipe)), item.CookedAt))
                .ToArray());
    }

    public async Task<RecipeResponse> CreateAsync(
        CreateRecipeRequest request,
        CancellationToken cancellationToken)
    {
        var recipe = await CreateRecipeAsync(request, cancellationToken);
        await repository.AddAsync(recipe, cancellationToken);
        if (recipe.IsPublished)
        {
            await TranslateAsync(recipe, cancellationToken);
        }

        return (await GetByIdAsync(recipe.Id, null, cancellationToken))!;
    }

    public async Task<RecipeResponse> ImportAsync(
        ImportRecipeRequest request,
        CancellationToken cancellationToken)
    {
        var recipe = await CreateRecipeAsync(
            request,
            cancellationToken,
            isPublished: false,
            sourceUrl: request.SourceUrl);
        await repository.AddAsync(recipe, cancellationToken);
        return (await GetByIdAsync(recipe.Id, null, cancellationToken))!;
    }

    public async Task<IReadOnlyList<RecipeResponse>> GetDraftsAsync(
        string? language,
        CancellationToken cancellationToken)
    {
        var drafts = await repository.GetDraftsAsync(cancellationToken);
        var map = await CreateMapperAsync(drafts, language, cancellationToken);
        return drafts.Select(map).ToArray();
    }

    public async Task<RecipeResponse?> UpdateAsync(
        Guid id,
        UpdateRecipeRequest request,
        CancellationToken cancellationToken)
    {
        var current = await repository.GetByIdAsync(id, cancellationToken);
        if (current is null)
        {
            return null;
        }

        var recipe = await CreateRecipeAsync(request, cancellationToken, id, current.Recipe.AddedAt);
        if (!await repository.UpdateAsync(recipe, cancellationToken))
        {
            return null;
        }

        if (recipe.IsPublished)
        {
            await TranslateAsync(recipe, cancellationToken);
        }

        return await GetByIdAsync(id, null, cancellationToken);
    }

    public async Task<RecipeResponse?> PublishAsync(Guid id, CancellationToken cancellationToken)
    {
        if (!await repository.PublishAsync(id, cancellationToken))
        {
            return null;
        }

        var published = await repository.GetByIdAsync(id, cancellationToken);
        if (published is not null)
        {
            await TranslateAsync(published.Recipe, cancellationToken);
        }

        return await GetByIdAsync(id, null, cancellationToken);
    }

    public async Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken)
    {
        if (!await repository.DeleteAsync(id, cancellationToken))
        {
            return false;
        }

        if (imageStorage.IsConfigured)
        {
            try
            {
                await imageStorage.DeleteAsync(ImageKey(id), cancellationToken);
            }
            catch (Exception exception) when (exception is not OperationCanceledException)
            {
                logger.LogWarning(exception, "Could not delete the image of recipe {RecipeId}.", id);
            }
        }

        return true;
    }

    public async Task<RecipeImageResult> SetImageAsync(
        Guid id,
        Stream content,
        CancellationToken cancellationToken)
    {
        if (!imageStorage.IsConfigured)
        {
            return RecipeImageResult.StorageUnavailable;
        }

        if (await repository.GetImageInfoAsync(id, cancellationToken) is null)
        {
            return RecipeImageResult.NotFound;
        }

        var buffer = new MemoryStream();
        await content.CopyToAsync(buffer, cancellationToken);
        var contentType = DetectImageType(buffer);
        if (contentType is null)
        {
            return RecipeImageResult.Invalid;
        }

        buffer.Position = 0;
        await imageStorage.PutAsync(ImageKey(id), buffer, contentType, cancellationToken);
        await repository.SetImageAsync(id, DateTimeOffset.UtcNow, cancellationToken);
        return RecipeImageResult.Saved;
    }

    public async Task<StoredImage?> GetImageAsync(Guid id, bool includeDrafts, CancellationToken cancellationToken)
    {
        if (!imageStorage.IsConfigured)
        {
            return null;
        }

        var info = await repository.GetImageInfoAsync(id, cancellationToken);
        if (info is null || info.ImageUpdatedAt is null || (!info.IsPublished && !includeDrafts))
        {
            return null;
        }

        return await imageStorage.GetAsync(ImageKey(id), cancellationToken);
    }

    public async Task<bool> DeleteImageAsync(Guid id, CancellationToken cancellationToken)
    {
        if (await repository.GetImageInfoAsync(id, cancellationToken) is null)
        {
            return false;
        }

        if (imageStorage.IsConfigured)
        {
            await imageStorage.DeleteAsync(ImageKey(id), cancellationToken);
        }

        await repository.SetImageAsync(id, null, cancellationToken);
        return true;
    }

    private static string ImageKey(Guid id) => $"recipes/{id}";

    // Only real JPEG, PNG and WebP files are accepted; the type is taken from the bytes, not the client.
    private static string? DetectImageType(MemoryStream stream)
    {
        var bytes = stream.GetBuffer().AsSpan(0, (int)stream.Length);
        if (bytes.Length >= 3 && bytes[0] == 0xFF && bytes[1] == 0xD8 && bytes[2] == 0xFF)
        {
            return "image/jpeg";
        }

        if (bytes.Length >= 8 && bytes[..8].SequenceEqual(new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A }))
        {
            return "image/png";
        }

        if (bytes.Length >= 12
            && bytes[..4].SequenceEqual("RIFF"u8)
            && bytes[8..12].SequenceEqual("WEBP"u8))
        {
            return "image/webp";
        }

        return null;
    }

    public Task<RecipeCookedResponse?> LogCookedAsync(
        Guid recipeId,
        string userId,
        LogRecipeCookedRequest request,
        CancellationToken cancellationToken) =>
        repository.LogCookedAsync(recipeId, userId, request, cancellationToken);

    public Task<IReadOnlyList<RecipeCookedResponse>> GetCookHistoryAsync(
        string userId,
        Guid? recipeId,
        CancellationToken cancellationToken) =>
        repository.GetCookHistoryAsync(userId, recipeId, cancellationToken);

    public Task<RecipeCookedResponse?> UpdateCookLogAsync(
        Guid logId,
        string userId,
        LogRecipeCookedRequest request,
        CancellationToken cancellationToken) =>
        repository.UpdateCookLogAsync(logId, userId, request, cancellationToken);

    public Task<bool> DeleteCookLogAsync(Guid logId, string userId, CancellationToken cancellationToken) =>
        repository.DeleteCookLogAsync(logId, userId, cancellationToken);

    private async Task TranslateAsync(Recipe recipe, CancellationToken cancellationToken)
    {
        if (!translator.IsConfigured)
        {
            return;
        }

        try
        {
            var target = RecipeLanguages.Other(recipe.Language);
            var hash = RecipeTexts.Hash(recipe);
            var existing = await repository.GetTranslationsAsync([recipe.Id], target, cancellationToken);
            if (existing.TryGetValue(recipe.Id, out var current) && current.SourceHash == hash)
            {
                return;
            }

            var source = RecipeTexts.Extract(recipe);
            var indexes = Enumerable.Range(0, source.Count)
                .Where(index => !string.IsNullOrWhiteSpace(source[index]))
                .ToArray();
            var translated = await translator.TranslateAsync(
                indexes.Select(index => source[index]!).ToArray(),
                recipe.Language,
                target,
                cancellationToken);
            if (translated.Count != indexes.Length)
            {
                throw new InvalidOperationException("Translator returned an unexpected number of texts.");
            }

            var values = new string?[source.Count];
            for (var i = 0; i < indexes.Length; i++)
            {
                values[indexes[i]] = translated[i];
            }

            var content = JsonSerializer.Serialize(RecipeTexts.Build(recipe, values));
            await repository.SaveTranslationAsync(
                RecipeTranslation.Create(recipe.Id, target, content, hash),
                cancellationToken);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            // A translation failure must never block saving or publishing a recipe.
            logger.LogWarning(exception, "Could not translate recipe {RecipeId}.", recipe.Id);
        }
    }

    private async Task<Func<RecipeView, RecipeResponse>> CreateMapperAsync(
        IReadOnlyCollection<RecipeView> views,
        string? language,
        CancellationToken cancellationToken)
    {
        var requested = RecipeLanguages.Normalize(language);
        var catalogIngredients = (await catalog.GetIngredientsAsync(cancellationToken))
            .ToDictionary(ingredient => ingredient.Id);
        if (requested is null)
        {
            return view => ToResponse(view, catalogIngredients);
        }

        var translations = await repository.GetTranslationsAsync(
            views.Where(view => view.Recipe.Language != requested)
                .Select(view => view.Recipe.Id)
                .Distinct()
                .ToArray(),
            requested,
            cancellationToken);
        return view => ToResponse(
            view,
            catalogIngredients,
            translations.TryGetValue(view.Recipe.Id, out var translation) ? translation : null);
    }

    private async Task<Recipe> CreateRecipeAsync(
        RecipeWriteRequest request,
        CancellationToken cancellationToken,
        Guid? id = null,
        DateTimeOffset? addedAt = null,
        bool? isPublished = null,
        string? sourceUrl = null)
    {
        var language = RecipeLanguages.Normalize(request.Language) ?? "en";
        var terms = (await catalog.GetTermsAsync(cancellationToken))
            .Select(term => (term.Kind, term.Slug))
            .ToHashSet();
        var ingredients = (await catalog.GetIngredientsAsync(cancellationToken))
            .ToDictionary(ingredient => ingredient.Id);

        var recipeTerms = new List<RecipeTerm>();
        void AddTerms(string kind, IEnumerable<string> slugs)
        {
            foreach (var slug in slugs.Select(value => value.Trim()).Distinct())
            {
                if (!terms.Contains((kind, slug)))
                {
                    throw new RecipeValidationException($"Unknown {kind} value '{slug}'.");
                }

                recipeTerms.Add(RecipeTerm.Create(kind, slug));
            }
        }

        AddTerms(TermKinds.MealType, request.MealTypes);
        AddTerms(TermKinds.Course, request.Courses);
        AddTerms(TermKinds.CookingMethod, request.CookingMethods);
        AddTerms(TermKinds.Cuisine, request.Cuisines);
        AddTerms(TermKinds.Tag, request.Tags);

        var recipeIngredients = request.Ingredients.Select((ingredient, position) =>
        {
            if (ingredient.IngredientId is not { } ingredientId
                || !ingredients.TryGetValue(ingredientId, out var known))
            {
                throw new RecipeValidationException("Every ingredient must exist in the ingredient list.");
            }

            return RecipeIngredient.Create(
                known.Id,
                known.NameFor(language),
                ingredient.Quantity,
                ingredient.Unit,
                ingredient.Section,
                ingredient.Notes,
                position);
        }).ToList();

        return Recipe.Create(
            request.Name,
            request.Description,
            request.Servings,
            request.PrepTimeMinutes,
            request.CookTimeMinutes,
            request.CaloriesPerServing,
            request.ProteinGramsPerServing,
            request.CarbohydratesGramsPerServing,
            request.FatGramsPerServing,
            recipeIngredients,
            request.Steps.Select((step, position) => RecipeStep.Create(step, position)),
            request.Equipment.Select((item, position) => RecipeEquipment.Create(item, position)),
            recipeTerms,
            id,
            addedAt,
            isPublished ?? request.IsPublished,
            sourceUrl ?? request.SourceUrl,
            language);
    }

    private static RecipeResponse ToResponse(
        RecipeView view,
        IReadOnlyDictionary<Guid, Ingredient> catalogIngredients,
        RecipeTranslation? translation = null)
    {
        var recipe = view.Recipe;
        var content = ReadContent(recipe, translation);
        var displayLanguage = content is null ? recipe.Language : translation!.Language;
        var ingredients = recipe.Ingredients.OrderBy(ingredient => ingredient.Position).ToArray();
        return new RecipeResponse(
            recipe.Id,
            content?.Name ?? recipe.Name,
            content is null ? recipe.Description : content.Description,
            recipe.Servings,
            recipe.PrepTimeMinutes,
            recipe.CookTimeMinutes,
            recipe.TermsOf(TermKinds.MealType).ToArray(),
            recipe.TermsOf(TermKinds.Course).ToArray(),
            recipe.TermsOf(TermKinds.CookingMethod).ToArray(),
            recipe.TermsOf(TermKinds.Cuisine).ToArray(),
            recipe.CaloriesPerServing,
            recipe.ProteinGramsPerServing,
            recipe.CarbohydratesGramsPerServing,
            recipe.FatGramsPerServing,
            recipe.AddedAt,
            recipe.IsPublished,
            recipe.SourceUrl,
            view.CookCount,
            view.RecentCookCount,
            view.AverageRating,
            recipe.TermsOf(TermKinds.Tag).ToArray(),
            ingredients
                .Select((ingredient, index) =>
                {
                    var translated = content?.Ingredients[index];
                    var name = ingredient.IngredientId is { } ingredientId
                        && catalogIngredients.TryGetValue(ingredientId, out var known)
                            ? known.NameFor(displayLanguage)
                            : translated?.Name ?? ingredient.Name;
                    return new RecipeIngredientResponse(
                        name,
                        ingredient.Quantity,
                        translated?.Unit ?? ingredient.Unit,
                        content is null ? ingredient.Section : translated?.Section,
                        content is null ? ingredient.Notes : translated?.Notes,
                        ingredient.IngredientId);
                })
                .ToArray(),
            content?.Steps
                ?? recipe.Steps.OrderBy(step => step.Position).Select(step => step.Instructions).ToList(),
            content?.Equipment
                ?? recipe.Equipment.OrderBy(item => item.Position).Select(item => item.Name).ToList(),
            recipe.Language,
            displayLanguage,
            content is not null,
            recipe.ImageUpdatedAt is { } imageUpdatedAt
                ? $"recipes/{recipe.Id}/image?v={imageUpdatedAt.ToUnixTimeMilliseconds()}"
                : null,
            ingredients.Length,
            recipe.Steps.Count);
    }

    // Lists and the dashboard only carry what a card needs, so one request cannot dump the whole
    // library. The full recipe has to be fetched one by one, which the rate limits cover.
    private static RecipeResponse ToSummary(RecipeResponse recipe) =>
        recipe with { Ingredients = [], Steps = [], Equipment = [], SourceUrl = null };

    // A stored translation is only used while it still matches the current recipe text.
    private static TranslatedContent? ReadContent(Recipe recipe, RecipeTranslation? translation)
    {
        if (translation is null || translation.SourceHash != RecipeTexts.Hash(recipe))
        {
            return null;
        }

        try
        {
            var content = JsonSerializer.Deserialize<TranslatedContent>(translation.Content);
            return content is not null
                && content.Ingredients.Count == recipe.Ingredients.Count
                && content.Steps.Count == recipe.Steps.Count
                && content.Equipment.Count == recipe.Equipment.Count
                    ? content
                    : null;
        }
        catch (JsonException)
        {
            return null;
        }
    }
}
