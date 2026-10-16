using MealBook.Domain.Recipes;

namespace MealBook.Application.Planning;

public sealed record PlanSourceLine(
    Guid? IngredientId,
    string Name,
    string NameDa,
    string SectionSlug,
    decimal Quantity,
    string Unit);

public sealed record PlanSourceEntry(
    Guid EntryId,
    Guid RecipeId,
    string RecipeName,
    int RecipeServings,
    int Servings,
    DateTimeOffset? ImageUpdatedAt,
    DateTimeOffset AddedAt,
    IReadOnlyList<PlanSourceLine> Lines);

public sealed record PerishableUsage(
    Guid RecipeId,
    string RecipeName,
    DateTimeOffset? ImageUpdatedAt,
    Guid IngredientId,
    string IngredientNameEn,
    string IngredientNameDa);

public sealed record ShoppingCheckKey(Guid IngredientId, string Unit);

public enum PlanChangeResult
{
    Changed,
    NotFound
}

public interface IMealPlanRepository
{
    Task<IReadOnlyList<PlanSourceEntry>> GetEntriesAsync(CancellationToken cancellationToken);

    Task<IReadOnlySet<ShoppingCheckKey>> GetChecksAsync(CancellationToken cancellationToken);

    // Perishable ingredient usages of every published recipe, used for suggestions.
    Task<IReadOnlyList<PerishableUsage>> GetPerishableUsagesAsync(CancellationToken cancellationToken);

    Task<int> CountPublishedRecipesAsync(CancellationToken cancellationToken);

    Task<PlanChangeResult> AddAsync(Guid recipeId, int? servings, string userId, CancellationToken cancellationToken);

    Task<PlanChangeResult> SetServingsAsync(Guid recipeId, int servings, CancellationToken cancellationToken);

    Task<PlanChangeResult> RemoveAsync(Guid recipeId, CancellationToken cancellationToken);

    Task ClearAsync(CancellationToken cancellationToken);

    Task SetCheckedAsync(
        Guid ingredientId,
        string unit,
        bool isChecked,
        string userId,
        CancellationToken cancellationToken);

    Task ClearChecksAsync(CancellationToken cancellationToken);
}

// Tells every running instance (and so every connected browser) that the plan changed.
public interface IMealPlanNotifier
{
    Task PlanChangedAsync(CancellationToken cancellationToken);
}

public interface IPlanChangeBroadcaster
{
    Task BroadcastAsync(CancellationToken cancellationToken);
}

public sealed record PlanEntryResponse(
    Guid RecipeId,
    string RecipeName,
    string? ImageUrl,
    int Servings,
    int RecipeServings);

public sealed record ShoppingItemResponse(
    Guid? IngredientId,
    string Name,
    decimal Quantity,
    string Unit,
    bool Checked,
    IReadOnlyList<string> Recipes);

public sealed record ShoppingSectionResponse(
    string Slug,
    string Name,
    IReadOnlyList<ShoppingItemResponse> Items);

public sealed record MealPlanResponse(
    IReadOnlyList<PlanEntryResponse> Entries,
    IReadOnlyList<ShoppingSectionResponse> Sections);

public sealed record PlanSuggestionResponse(
    Guid RecipeId,
    string Name,
    string? ImageUrl,
    IReadOnlyList<string> SharedIngredients,
    int NewPerishables,
    double Score);

public sealed class PlanAddRequest
{
    [System.ComponentModel.DataAnnotations.Range(1, 100)]
    public int? Servings { get; init; }
}

public sealed class PlanServingsRequest
{
    [System.ComponentModel.DataAnnotations.Range(1, 100)]
    public int Servings { get; init; }
}

public sealed class ShoppingCheckRequest
{
    public Guid IngredientId { get; init; }

    [System.ComponentModel.DataAnnotations.Required]
    [System.ComponentModel.DataAnnotations.StringLength(40, MinimumLength = 1)]
    public string Unit { get; init; } = string.Empty;

    public bool Checked { get; init; }
}

public interface IMealPlanService
{
    Task<MealPlanResponse> GetPlanAsync(string? language, CancellationToken cancellationToken);

    Task<IReadOnlyList<PlanSuggestionResponse>> GetSuggestionsAsync(
        string? language,
        CancellationToken cancellationToken);

    Task<bool> AddAsync(Guid recipeId, int? servings, string userId, CancellationToken cancellationToken);

    Task<bool> SetServingsAsync(Guid recipeId, int servings, CancellationToken cancellationToken);

    Task<bool> RemoveAsync(Guid recipeId, CancellationToken cancellationToken);

    Task ClearAsync(CancellationToken cancellationToken);

    Task SetCheckedAsync(
        Guid ingredientId,
        string unit,
        bool isChecked,
        string userId,
        CancellationToken cancellationToken);

    Task ClearChecksAsync(CancellationToken cancellationToken);
}
