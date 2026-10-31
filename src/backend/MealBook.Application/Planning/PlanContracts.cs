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
    IReadOnlyList<PlanSourceLine> Lines,
    DateOnly? PlannedStart = null,
    int PlannedDays = 1);

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

    Task<IReadOnlyDictionary<ShoppingCheckKey, decimal>> GetHavesAsync(CancellationToken cancellationToken);

    Task<IReadOnlyList<MealBook.Domain.Planning.ShoppingCustomItem>> GetCustomItemsAsync(
        CancellationToken cancellationToken);

    Task AddCustomItemAsync(
        string name,
        decimal quantity,
        string unit,
        string sectionSlug,
        string userId,
        CancellationToken cancellationToken);

    Task<bool> SetCustomItemCheckedAsync(Guid id, bool isChecked, CancellationToken cancellationToken);

    Task<bool> RemoveCustomItemAsync(Guid id, CancellationToken cancellationToken);

    // A quantity of zero or less removes the amount.
    Task SetHaveAsync(
        Guid ingredientId,
        string unit,
        decimal quantity,
        string userId,
        CancellationToken cancellationToken);

    // Perishable ingredient usages of every published recipe, used for suggestions.
    Task<IReadOnlyList<PerishableUsage>> GetPerishableUsagesAsync(CancellationToken cancellationToken);

    Task<int> CountPublishedRecipesAsync(CancellationToken cancellationToken);

    Task<PlanChangeResult> AddAsync(Guid recipeId, int? servings, string userId, CancellationToken cancellationToken);

    Task<PlanChangeResult> SetServingsAsync(Guid recipeId, int servings, CancellationToken cancellationToken);

    Task<PlanChangeResult> RemoveAsync(Guid recipeId, CancellationToken cancellationToken);

    // A null start unschedules the recipe.
    Task<PlanChangeResult> ScheduleAsync(
        Guid recipeId,
        DateOnly? start,
        int days,
        CancellationToken cancellationToken);

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
    int RecipeServings,
    DateOnly? PlannedStart = null,
    int PlannedDays = 1);

public sealed record ShoppingAmountResponse(decimal Quantity, string Unit);

// One recipe's contribution to a shopping line, scaled to the planned servings in the recipe's own unit.
public sealed record ShoppingSourceResponse(string Recipe, decimal Quantity, string Unit);

// Quantity and Unit identify the line for checks and "have" amounts. A line whose units cannot be
// added together (e.g. grams and cans) uses PlanUnits.Mixed with a quantity of 1, so "have" is a share.
public sealed record ShoppingItemResponse(
    Guid? IngredientId,
    string Name,
    decimal Quantity,
    string Unit,
    bool Checked,
    decimal Have,
    IReadOnlyList<string> Recipes,
    Guid? CustomId = null,
    IReadOnlyList<ShoppingAmountResponse>? Amounts = null,
    IReadOnlyList<ShoppingSourceResponse>? Sources = null,
    bool PantryItem = false,
    bool PantryOverride = false);

public sealed record ShoppingSectionResponse(
    string Slug,
    string Name,
    IReadOnlyList<ShoppingItemResponse> Items);

public sealed record MealPlanResponse(
    IReadOnlyList<PlanEntryResponse> Entries,
    IReadOnlyList<ShoppingSectionResponse> Sections,
    bool UsePantry = false);

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

public sealed class PlanScheduleRequest
{
    // Null moves the recipe back to the unscheduled tray.
    public DateOnly? Start { get; init; }

    [System.ComponentModel.DataAnnotations.Range(1, 14)]
    public int Days { get; init; } = 1;
}

public sealed class ShoppingCheckRequest
{
    public Guid IngredientId { get; init; }

    [System.ComponentModel.DataAnnotations.Required]
    [System.ComponentModel.DataAnnotations.StringLength(40, MinimumLength = 1)]
    public string Unit { get; init; } = string.Empty;

    public bool Checked { get; init; }
}

public sealed class ShoppingHaveRequest
{
    public Guid IngredientId { get; init; }

    [System.ComponentModel.DataAnnotations.Required]
    [System.ComponentModel.DataAnnotations.StringLength(40, MinimumLength = 1)]
    public string Unit { get; init; } = string.Empty;

    [System.ComponentModel.DataAnnotations.Range(0, 1000000)]
    public decimal Quantity { get; init; }
}

public sealed class ShoppingCustomItemRequest
{
    [System.ComponentModel.DataAnnotations.Required]
    [System.ComponentModel.DataAnnotations.StringLength(120, MinimumLength = 1)]
    public string Name { get; init; } = string.Empty;

    [System.ComponentModel.DataAnnotations.Range(0, 1000000)]
    public decimal Quantity { get; init; }

    [System.ComponentModel.DataAnnotations.StringLength(40)]
    public string Unit { get; init; } = string.Empty;

    [System.ComponentModel.DataAnnotations.Required]
    [System.ComponentModel.DataAnnotations.StringLength(20, MinimumLength = 1)]
    public string SectionSlug { get; init; } = string.Empty;
}

public sealed class ShoppingCustomCheckRequest
{
    public bool Checked { get; init; }
}

public interface IMealPlanService
{
    // False when the section is unknown or the name is blank.
    Task<bool> AddCustomItemAsync(
        ShoppingCustomItemRequest request,
        string userId,
        CancellationToken cancellationToken);

    Task<bool> SetCustomItemCheckedAsync(Guid id, bool isChecked, CancellationToken cancellationToken);

    Task<bool> RemoveCustomItemAsync(Guid id, CancellationToken cancellationToken);

    Task SetHaveAsync(
        Guid ingredientId,
        string unit,
        decimal quantity,
        string userId,
        CancellationToken cancellationToken);

    Task<MealPlanResponse> GetPlanAsync(string? language, CancellationToken cancellationToken);

    Task<IReadOnlyList<PlanSuggestionResponse>> GetSuggestionsAsync(
        string? language,
        CancellationToken cancellationToken);

    Task<bool> AddAsync(Guid recipeId, int? servings, string userId, CancellationToken cancellationToken);

    Task<bool> SetServingsAsync(Guid recipeId, int servings, CancellationToken cancellationToken);

    Task<bool> RemoveAsync(Guid recipeId, CancellationToken cancellationToken);

    Task<bool> ScheduleAsync(Guid recipeId, DateOnly? start, int days, CancellationToken cancellationToken);

    Task ClearAsync(CancellationToken cancellationToken);

    Task SetCheckedAsync(
        Guid ingredientId,
        string unit,
        bool isChecked,
        string userId,
        CancellationToken cancellationToken);

    Task ClearChecksAsync(CancellationToken cancellationToken);
}
