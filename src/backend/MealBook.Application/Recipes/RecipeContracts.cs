namespace MealBook.Application.Recipes;

public sealed record RecipeIngredientResponse(
    string Name,
    decimal Quantity,
    string Unit,
    string? Section = null,
    string? Notes = null,
    Guid? IngredientId = null);

public sealed record RecipeResponse(
    Guid Id,
    string Name,
    string? Description,
    int Servings,
    int PrepTimeMinutes,
    int CookTimeMinutes,
    IReadOnlyList<string> MealTypes,
    IReadOnlyList<string> Courses,
    IReadOnlyList<string> CookingMethods,
    IReadOnlyList<string> Cuisines,
    int? CaloriesPerServing,
    int? ProteinGramsPerServing,
    int? CarbohydratesGramsPerServing,
    int? FatGramsPerServing,
    DateTimeOffset AddedAt,
    bool IsPublished,
    string? SourceUrl,
    int CookCount,
    int RecentCookCount,
    double? AverageRating,
    IReadOnlyList<string> Tags,
    IReadOnlyList<RecipeIngredientResponse> Ingredients,
    IReadOnlyList<string> Steps,
    IReadOnlyList<string> Equipment,
    string Language = "en",
    string DisplayLanguage = "en",
    bool IsTranslated = false,
    string? ImageUrl = null,
    int IngredientCount = 0,
    int StepCount = 0,
    int RatingCount = 0);

public enum RecipeImageResult
{
    Saved,
    NotFound,
    Invalid,
    StorageUnavailable
}

public sealed record RecipeImageInfo(bool IsPublished, DateTimeOffset? ImageUpdatedAt);

public sealed class CreateRecipeRequest : RecipeWriteRequest
{
}

public sealed class UpdateRecipeRequest : RecipeWriteRequest
{
}

public abstract class RecipeWriteRequest
{
    [System.ComponentModel.DataAnnotations.Required]
    [System.ComponentModel.DataAnnotations.StringLength(200, MinimumLength = 1)]
    public string Name { get; init; } = string.Empty;

    [System.ComponentModel.DataAnnotations.StringLength(2000)]
    public string? Description { get; init; }

    [System.ComponentModel.DataAnnotations.Range(1, 100)]
    public int Servings { get; init; } = 1;

    [System.ComponentModel.DataAnnotations.Range(0, 1440)]
    public int PrepTimeMinutes { get; init; }

    [System.ComponentModel.DataAnnotations.Range(0, 1440)]
    public int CookTimeMinutes { get; init; }

    [StringItems(80)]
    [System.ComponentModel.DataAnnotations.MaxLength(20)]
    public List<string> MealTypes { get; init; } = [];

    [StringItems(80)]
    [System.ComponentModel.DataAnnotations.MaxLength(20)]
    public List<string> Courses { get; init; } = [];

    [StringItems(80)]
    [System.ComponentModel.DataAnnotations.MaxLength(20)]
    public List<string> CookingMethods { get; init; } = [];

    [StringItems(80)]
    [System.ComponentModel.DataAnnotations.MaxLength(20)]
    public List<string> Cuisines { get; init; } = [];

    [System.ComponentModel.DataAnnotations.Range(0, 10000)]
    public int? CaloriesPerServing { get; init; }

    [System.ComponentModel.DataAnnotations.Range(0, 1000)]
    public int? ProteinGramsPerServing { get; init; }

    [System.ComponentModel.DataAnnotations.Range(0, 1000)]
    public int? CarbohydratesGramsPerServing { get; init; }

    [System.ComponentModel.DataAnnotations.Range(0, 1000)]
    public int? FatGramsPerServing { get; init; }

    public bool IsPublished { get; init; } = true;

    [System.ComponentModel.DataAnnotations.StringLength(2048)]
    public string? SourceUrl { get; init; }

    [System.ComponentModel.DataAnnotations.RegularExpression("^(en|da)$")]
    public string Language { get; init; } = "en";

    [StringItems(80)]
    [System.ComponentModel.DataAnnotations.MaxLength(30)]
    public List<string> Tags { get; init; } = [];

    [System.ComponentModel.DataAnnotations.MinLength(1)]
    public List<CreateRecipeIngredientRequest> Ingredients { get; init; } = [];

    [System.ComponentModel.DataAnnotations.MaxLength(100)]
    [System.ComponentModel.DataAnnotations.MinLength(1)]
    [StringItems(4000)]
    public List<string> Steps { get; init; } = [];

    [System.ComponentModel.DataAnnotations.MaxLength(30)]
    [StringItems(200)]
    public List<string> Equipment { get; init; } = [];
}

public sealed class ImportRecipeRequest : RecipeWriteRequest
{
}

[AttributeUsage(AttributeTargets.Property)]
public sealed class StringItemsAttribute(int maxItemLength) : System.ComponentModel.DataAnnotations.ValidationAttribute
{
    public override bool IsValid(object? value) =>
        value is IEnumerable<string> items
        && items.All(item => item is not null
            && !string.IsNullOrWhiteSpace(item)
            && item.Length <= maxItemLength);
}

public sealed class CreateRecipeIngredientRequest
{
    [System.ComponentModel.DataAnnotations.Required]
    public Guid? IngredientId { get; init; }

    [System.ComponentModel.DataAnnotations.Range(typeof(decimal), "0.001", "1000000", ParseLimitsInInvariantCulture = true, ConvertValueInInvariantCulture = true)]
    public decimal Quantity { get; init; }

    [System.ComponentModel.DataAnnotations.Required]
    public string Unit { get; init; } = string.Empty;

    [System.ComponentModel.DataAnnotations.Required]
    public string Section { get; init; } = string.Empty;

    [System.ComponentModel.DataAnnotations.StringLength(500)]
    public string? Notes { get; init; }
}

public sealed class RecipeSearchRequest
{
    public string? Search { get; init; }
    public int? MaxTotalMinutes { get; init; }
    public int? MinTotalMinutes { get; init; }
    public List<string> MealTypes { get; init; } = [];
    public List<string> Courses { get; init; } = [];
    public List<string> CookingMethods { get; init; } = [];
    public List<string> Cuisines { get; init; } = [];
    public List<Guid> Ingredients { get; init; } = [];
    public List<string> Tags { get; init; } = [];
    public int? MaxCalories { get; init; }
    public double? MinRating { get; init; }
    public string Sort { get; init; } = "newest";
    public string? Lang { get; init; }
}

public sealed record RecipeSuggestionResponse(Guid Id, string Name);

public sealed record TermResponse(string Kind, string Slug, string NameEn, string NameDa);

public sealed record ShopSectionResponse(string Slug, string NameEn, string NameDa, int Position, bool Perishable);

public sealed record IngredientResponse(Guid Id, string NameEn, string NameDa, string ShopSectionSlug);

public sealed class IngredientWriteRequest
{
    [System.ComponentModel.DataAnnotations.StringLength(20)]
    public string? ShopSectionSlug { get; init; }

    [System.ComponentModel.DataAnnotations.Required]
    [System.ComponentModel.DataAnnotations.StringLength(200, MinimumLength = 1)]
    public string NameEn { get; init; } = string.Empty;

    [System.ComponentModel.DataAnnotations.Required]
    [System.ComponentModel.DataAnnotations.StringLength(200, MinimumLength = 1)]
    public string NameDa { get; init; } = string.Empty;
}

public sealed record RecipeCookedResponse(
    Guid Id,
    Guid RecipeId,
    string RecipeName,
    DateTimeOffset CookedAt,
    int? Rating,
    string? Notes);

public sealed record RecentlyCookedRecipeResponse(RecipeResponse Recipe, DateTimeOffset CookedAt);

public sealed record RecipeDashboardResponse(
    IReadOnlyList<RecipeResponse> RecentlyAdded,
    IReadOnlyList<RecipeResponse> MostCooked,
    IReadOnlyList<RecentlyCookedRecipeResponse> RecentlyCooked);

public sealed record RecipeDashboardData(
    IReadOnlyList<RecipeView> RecentlyAdded,
    IReadOnlyList<RecipeView> MostCooked,
    IReadOnlyList<RecentlyCookedRecipeData> RecentlyCooked);

public sealed record RecentlyCookedRecipeData(RecipeView Recipe, DateTimeOffset CookedAt);

public sealed class LogRecipeCookedRequest
{
    [System.ComponentModel.DataAnnotations.Range(1, 5)]
    public int? Rating { get; init; }

    [System.ComponentModel.DataAnnotations.StringLength(1000)]
    public string? Notes { get; init; }
}
