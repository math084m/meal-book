using System.Text.Json;
using MealBook.Domain.Recipes;
using Microsoft.Extensions.Logging;

namespace MealBook.Application.Recipes;

public sealed class RecipeService(
    IRecipeRepository repository,
    ITranslator translator,
    ILogger<RecipeService> logger) : IRecipeService
{
    public async Task<IReadOnlyList<RecipeResponse>> GetAllAsync(
        RecipeSearchRequest search,
        CancellationToken cancellationToken)
    {
        var recipes = await repository.GetAllAsync(search, cancellationToken);
        var map = await CreateMapperAsync(recipes, search.Lang, cancellationToken);
        return recipes.Select(map).ToArray();
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
            dashboard.RecentlyAdded.Select(map).ToArray(),
            dashboard.MostCooked.Select(map).ToArray(),
            dashboard.RecentlyCooked
                .Select(item => new RecentlyCookedRecipeResponse(map(item.Recipe), item.CookedAt))
                .ToArray());
    }

    public async Task<RecipeResponse> CreateAsync(
        CreateRecipeRequest request,
        CancellationToken cancellationToken)
    {
        var recipe = CreateRecipe(request);
        await repository.AddAsync(recipe, cancellationToken);
        if (recipe.IsPublished)
        {
            await TranslateAsync(recipe, cancellationToken);
        }

        return ToResponse(new RecipeView(recipe, 0, null));
    }

    public async Task<RecipeResponse> ImportAsync(
        ImportRecipeRequest request,
        CancellationToken cancellationToken)
    {
        var recipe = CreateRecipe(request, isPublished: false, sourceUrl: request.SourceUrl);
        await repository.AddAsync(recipe, cancellationToken);
        return ToResponse(new RecipeView(recipe, 0, null));
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

        var recipe = CreateRecipe(request, id, current.Recipe.AddedAt);
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

    public Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken) =>
        repository.DeleteAsync(id, cancellationToken);

    public Task<RecipeCookedResponse?> LogCookedAsync(
        Guid recipeId,
        string userId,
        LogRecipeCookedRequest request,
        CancellationToken cancellationToken) =>
        repository.LogCookedAsync(recipeId, userId, request, cancellationToken);

    public Task<IReadOnlyList<RecipeCookedResponse>> GetCookHistoryAsync(
        string userId,
        CancellationToken cancellationToken) =>
        repository.GetCookHistoryAsync(userId, cancellationToken);

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
        if (requested is null)
        {
            return view => ToResponse(view);
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
            translations.TryGetValue(view.Recipe.Id, out var translation) ? translation : null);
    }

    private static Recipe CreateRecipe(
        RecipeWriteRequest request,
        Guid? id = null,
        DateTimeOffset? addedAt = null,
        bool? isPublished = null,
        string? sourceUrl = null) =>
        Recipe.Create(
            request.Name,
            request.Description,
            request.Servings,
            request.PrepTimeMinutes,
            request.CookTimeMinutes,
            request.MealType,
            request.Course,
            request.CookingMethod,
            request.Cuisine,
            request.CaloriesPerServing,
            request.ProteinGramsPerServing,
            request.CarbohydratesGramsPerServing,
            request.FatGramsPerServing,
            request.Ingredients.Select((ingredient, position) =>
                RecipeIngredient.Create(
                    ingredient.Name,
                    ingredient.Quantity,
                    ingredient.Unit,
                    ingredient.Section,
                    ingredient.Notes,
                    position)),
            request.Steps.Select((step, position) => RecipeStep.Create(step, position)),
            request.Equipment.Select((item, position) => RecipeEquipment.Create(item, position)),
            request.Tags,
            id,
            addedAt,
            isPublished ?? request.IsPublished,
            sourceUrl ?? request.SourceUrl,
            RecipeLanguages.Normalize(request.Language) ?? "en");

    private static RecipeResponse ToResponse(RecipeView view, RecipeTranslation? translation = null)
    {
        var recipe = view.Recipe;
        var content = ReadContent(recipe, translation);
        var ingredients = recipe.Ingredients.OrderBy(ingredient => ingredient.Position).ToArray();
        return new RecipeResponse(
            recipe.Id,
            content?.Name ?? recipe.Name,
            content is null ? recipe.Description : content.Description,
            recipe.Servings,
            recipe.PrepTimeMinutes,
            recipe.CookTimeMinutes,
            recipe.MealType,
            recipe.Course,
            recipe.CookingMethod,
            recipe.Cuisine,
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
            content?.Tags ?? recipe.Tags.OrderBy(tag => tag.Position).Select(tag => tag.Name).ToList(),
            ingredients
                .Select((ingredient, index) =>
                {
                    var translated = content?.Ingredients[index];
                    return new RecipeIngredientResponse(
                        translated?.Name ?? ingredient.Name,
                        ingredient.Quantity,
                        translated?.Unit ?? ingredient.Unit,
                        content is null ? ingredient.Section : translated?.Section,
                        content is null ? ingredient.Notes : translated?.Notes);
                })
                .ToArray(),
            content?.Steps
                ?? recipe.Steps.OrderBy(step => step.Position).Select(step => step.Instructions).ToList(),
            content?.Equipment
                ?? recipe.Equipment.OrderBy(item => item.Position).Select(item => item.Name).ToList(),
            recipe.Language,
            content is null ? recipe.Language : translation!.Language,
            content is not null);
    }

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
                && content.Tags.Count == recipe.Tags.Count
                    ? content
                    : null;
        }
        catch (JsonException)
        {
            return null;
        }
    }
}
