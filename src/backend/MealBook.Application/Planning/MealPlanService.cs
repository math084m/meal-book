using MealBook.Application.Recipes;

namespace MealBook.Application.Planning;

public sealed class MealPlanService(
    IMealPlanRepository plan,
    ICatalogRepository catalog,
    IMealPlanNotifier notifier) : IMealPlanService
{
    private const int MaxSuggestions = 6;

    public async Task<MealPlanResponse> GetPlanAsync(string? language, CancellationToken cancellationToken)
    {
        var lang = NormalizeLanguage(language);
        var entries = await plan.GetEntriesAsync(cancellationToken);
        var checks = await plan.GetChecksAsync(cancellationToken);
        var sections = await catalog.GetShopSectionsAsync(cancellationToken);

        var totals = new Dictionary<(string Key, string Unit), Total>();
        foreach (var entry in entries)
        {
            var factor = (decimal)entry.Servings / Math.Max(entry.RecipeServings, 1);
            foreach (var line in entry.Lines)
            {
                var (quantity, unit) = PlanUnits.Normalize(line.Quantity * factor, line.Unit);
                var key = (line.IngredientId?.ToString() ?? "name:" + line.Name.Trim().ToLowerInvariant(), unit);
                if (!totals.TryGetValue(key, out var total))
                {
                    var name = lang == "da" && !string.IsNullOrWhiteSpace(line.NameDa) ? line.NameDa : line.Name;
                    total = new Total(line.IngredientId, name, unit, line.SectionSlug);
                    totals[key] = total;
                }

                total.Quantity += quantity;
                if (!total.Recipes.Contains(entry.RecipeName))
                {
                    total.Recipes.Add(entry.RecipeName);
                }
            }
        }

        var responseSections = new List<ShoppingSectionResponse>();
        foreach (var section in sections)
        {
            var items = totals.Values
                .Where(total => total.SectionSlug == section.Slug)
                .OrderBy(total => total.Name, StringComparer.CurrentCultureIgnoreCase)
                .ThenBy(total => total.Unit)
                .Select(total => new ShoppingItemResponse(
                    total.IngredientId,
                    total.Name,
                    Math.Round(total.Quantity, 3),
                    total.Unit,
                    total.IngredientId is { } id && checks.Contains(new ShoppingCheckKey(id, total.Unit)),
                    total.Recipes))
                .ToArray();
            if (items.Length > 0)
            {
                responseSections.Add(new ShoppingSectionResponse(section.Slug, section.NameFor(lang), items));
            }
        }

        var entryResponses = entries
            .OrderBy(entry => entry.AddedAt)
            .Select(entry => new PlanEntryResponse(
                entry.RecipeId,
                entry.RecipeName,
                ImageUrl(entry.RecipeId, entry.ImageUpdatedAt),
                entry.Servings,
                entry.RecipeServings))
            .ToArray();
        return new MealPlanResponse(entryResponses, responseSections);
    }

    // Recipes that reuse perishable ingredients already in the plan score highest. Ingredients that
    // appear in many recipes (onion, garlic) count for less than rare ones, and recipes that would
    // need many extra perishables are penalised.
    public async Task<IReadOnlyList<PlanSuggestionResponse>> GetSuggestionsAsync(
        string? language,
        CancellationToken cancellationToken)
    {
        var lang = NormalizeLanguage(language);
        var entries = await plan.GetEntriesAsync(cancellationToken);
        if (entries.Count == 0)
        {
            return [];
        }

        var usages = await plan.GetPerishableUsagesAsync(cancellationToken);
        var published = Math.Max(await plan.CountPublishedRecipesAsync(cancellationToken), 1);
        var plannedRecipes = entries.Select(entry => entry.RecipeId).ToHashSet();
        var plannedIngredients = usages
            .Where(usage => plannedRecipes.Contains(usage.RecipeId))
            .Select(usage => usage.IngredientId)
            .ToHashSet();
        if (plannedIngredients.Count == 0)
        {
            return [];
        }

        var weights = usages
            .GroupBy(usage => usage.IngredientId)
            .ToDictionary(
                group => group.Key,
                group => Math.Log(1 + (double)published / group.Select(usage => usage.RecipeId).Distinct().Count()));

        var suggestions = new List<PlanSuggestionResponse>();
        foreach (var recipe in usages.Where(usage => !plannedRecipes.Contains(usage.RecipeId)).GroupBy(usage => usage.RecipeId))
        {
            var ingredients = recipe.DistinctBy(usage => usage.IngredientId).ToArray();
            var shared = ingredients.Where(usage => plannedIngredients.Contains(usage.IngredientId)).ToArray();
            if (shared.Length == 0)
            {
                continue;
            }

            var fresh = ingredients.Length - shared.Length;
            var score = shared.Sum(usage => weights[usage.IngredientId])
                - 0.25 * ingredients.Where(usage => !plannedIngredients.Contains(usage.IngredientId))
                    .Sum(usage => weights[usage.IngredientId]);
            if (score <= 0)
            {
                continue;
            }

            var first = recipe.First();
            suggestions.Add(new PlanSuggestionResponse(
                first.RecipeId,
                first.RecipeName,
                ImageUrl(first.RecipeId, first.ImageUpdatedAt),
                shared
                    .OrderByDescending(usage => weights[usage.IngredientId])
                    .Select(usage => lang == "da" && !string.IsNullOrWhiteSpace(usage.IngredientNameDa)
                        ? usage.IngredientNameDa
                        : usage.IngredientNameEn)
                    .ToArray(),
                fresh,
                Math.Round(score, 2)));
        }

        return suggestions
            .OrderByDescending(item => item.Score)
            .ThenBy(item => item.Name)
            .Take(MaxSuggestions)
            .ToArray();
    }

    public async Task<bool> AddAsync(Guid recipeId, int? servings, string userId, CancellationToken cancellationToken)
    {
        var result = await plan.AddAsync(recipeId, servings, userId, cancellationToken);
        return await FinishAsync(result, cancellationToken);
    }

    public async Task<bool> SetServingsAsync(Guid recipeId, int servings, CancellationToken cancellationToken) =>
        await FinishAsync(await plan.SetServingsAsync(recipeId, servings, cancellationToken), cancellationToken);

    public async Task<bool> RemoveAsync(Guid recipeId, CancellationToken cancellationToken) =>
        await FinishAsync(await plan.RemoveAsync(recipeId, cancellationToken), cancellationToken);

    public async Task ClearAsync(CancellationToken cancellationToken)
    {
        await plan.ClearAsync(cancellationToken);
        await notifier.PlanChangedAsync(cancellationToken);
    }

    public async Task SetCheckedAsync(
        Guid ingredientId,
        string unit,
        bool isChecked,
        string userId,
        CancellationToken cancellationToken)
    {
        await plan.SetCheckedAsync(ingredientId, unit.Trim().ToLowerInvariant(), isChecked, userId, cancellationToken);
        await notifier.PlanChangedAsync(cancellationToken);
    }

    public async Task ClearChecksAsync(CancellationToken cancellationToken)
    {
        await plan.ClearChecksAsync(cancellationToken);
        await notifier.PlanChangedAsync(cancellationToken);
    }

    private async Task<bool> FinishAsync(PlanChangeResult result, CancellationToken cancellationToken)
    {
        if (result == PlanChangeResult.NotFound)
        {
            return false;
        }

        await notifier.PlanChangedAsync(cancellationToken);
        return true;
    }

    private static string NormalizeLanguage(string? language) => language == "da" ? "da" : "en";

    private static string? ImageUrl(Guid recipeId, DateTimeOffset? imageUpdatedAt) =>
        imageUpdatedAt is { } updated
            ? $"recipes/{recipeId}/image?v={updated.ToUnixTimeMilliseconds()}"
            : null;

    private sealed class Total(Guid? ingredientId, string name, string unit, string sectionSlug)
    {
        public Guid? IngredientId { get; } = ingredientId;
        public string Name { get; } = name;
        public string Unit { get; } = unit;
        public string SectionSlug { get; } = sectionSlug;
        public decimal Quantity { get; set; }
        public List<string> Recipes { get; } = [];
    }
}
