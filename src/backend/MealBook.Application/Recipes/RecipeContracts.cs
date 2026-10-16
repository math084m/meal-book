namespace MealBook.Application.Recipes;

public sealed record RecipeIngredientResponse(
    string Name,
    decimal Quantity,
    string Unit,
    string? Section = null,
    string? Notes = null);

public sealed record RecipeResponse(
    Guid Id,
    string Name,
    string? Description,
    int Servings,
    int PrepTimeMinutes,
    int CookTimeMinutes,
    string MealType,
    string Course,
    string CookingMethod,
    string Cuisine,
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
    bool IsTranslated = false);

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

    [System.ComponentModel.DataAnnotations.StringLength(80)]
    public string MealType { get; init; } = string.Empty;

    [System.ComponentModel.DataAnnotations.StringLength(80)]
    public string Course { get; init; } = string.Empty;

    [System.ComponentModel.DataAnnotations.StringLength(100)]
    public string CookingMethod { get; init; } = string.Empty;

    [System.ComponentModel.DataAnnotations.StringLength(100)]
    public string Cuisine { get; init; } = string.Empty;

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
    [System.ComponentModel.DataAnnotations.StringLength(200, MinimumLength = 1)]
    public string Name { get; init; } = string.Empty;

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
    public string? MealType { get; init; }
    public string? Course { get; init; }
    public string? CookingMethod { get; init; }
    public string? Cuisine { get; init; }
    public string? Ingredient { get; init; }
    public string? Tags { get; init; }
    public int? MaxCalories { get; init; }
    public double? MinRating { get; init; }
    public string Sort { get; init; } = "newest";
    public string? Lang { get; init; }
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

public sealed record CurrentUserResponse(string Id, string Email, bool IsAdmin);

public sealed class RegisterRequest
{
    [System.ComponentModel.DataAnnotations.Required]
    [System.ComponentModel.DataAnnotations.EmailAddress]
    [System.ComponentModel.DataAnnotations.StringLength(256)]
    public string Email { get; init; } = string.Empty;

    [System.ComponentModel.DataAnnotations.Required]
    [System.ComponentModel.DataAnnotations.StringLength(128, MinimumLength = 10)]
    public string Password { get; init; } = string.Empty;
}
