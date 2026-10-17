namespace MealBook.Domain.Recipes;

public sealed class Recipe
{
    private Recipe()
    {
    }

    public Guid Id { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public string? Description { get; private set; }
    public int Servings { get; private set; }
    public int PrepTimeMinutes { get; private set; }
    public int CookTimeMinutes { get; private set; }
    public int? CaloriesPerServing { get; private set; }
    public int? ProteinGramsPerServing { get; private set; }
    public int? CarbohydratesGramsPerServing { get; private set; }
    public int? FatGramsPerServing { get; private set; }
    public DateTimeOffset AddedAt { get; private set; }
    public bool IsPublished { get; private set; }
    public string? SourceUrl { get; private set; }
    public string Language { get; private set; } = "en";

    // Set when a cover image is stored; doubles as the cache-busting version of the image URL.
    public DateTimeOffset? ImageUpdatedAt { get; private set; }
    public List<RecipeIngredient> Ingredients { get; private set; } = [];
    public List<RecipeStep> Steps { get; private set; } = [];
    public List<RecipeEquipment> Equipment { get; private set; } = [];
    public List<RecipeTerm> Terms { get; private set; } = [];

    public IEnumerable<string> TermsOf(string kind) =>
        Terms.Where(term => term.Kind == kind).Select(term => term.Slug).OrderBy(slug => slug);

    public void Publish() => IsPublished = true;

    public void SetImage(DateTimeOffset? updatedAt) => ImageUpdatedAt = updatedAt;

    public static Recipe Create(
        string name,
        string? description,
        int servings,
        int prepTimeMinutes,
        int cookTimeMinutes,
        int? caloriesPerServing,
        int? proteinGramsPerServing,
        int? carbohydratesGramsPerServing,
        int? fatGramsPerServing,
        IEnumerable<RecipeIngredient> ingredients,
        IEnumerable<RecipeStep> steps,
        IEnumerable<RecipeEquipment> equipment,
        IEnumerable<RecipeTerm> terms,
        Guid? id = null,
        DateTimeOffset? addedAt = null,
        bool isPublished = true,
        string? sourceUrl = null,
        string language = "en") =>
        new()
        {
            Language = language,
            Id = id ?? Guid.NewGuid(),
            Name = name.Trim(),
            Description = description?.Trim(),
            Servings = servings,
            PrepTimeMinutes = prepTimeMinutes,
            CookTimeMinutes = cookTimeMinutes,
            CaloriesPerServing = caloriesPerServing,
            ProteinGramsPerServing = proteinGramsPerServing,
            CarbohydratesGramsPerServing = carbohydratesGramsPerServing,
            FatGramsPerServing = fatGramsPerServing,
            AddedAt = addedAt ?? DateTimeOffset.UtcNow,
            IsPublished = isPublished,
            SourceUrl = string.IsNullOrWhiteSpace(sourceUrl) ? null : sourceUrl.Trim(),
            Ingredients = ingredients.ToList(),
            Steps = steps.ToList(),
            Equipment = equipment.ToList(),
            Terms = terms
                .DistinctBy(term => (term.Kind, term.Slug))
                .ToList()
        };
}

public sealed class RecipeTranslation
{
    private RecipeTranslation()
    {
    }

    public Guid RecipeId { get; private set; }
    public string Language { get; private set; } = string.Empty;
    public string Content { get; private set; } = string.Empty;
    public string SourceHash { get; private set; } = string.Empty;
    public DateTimeOffset TranslatedAt { get; private set; }

    public static RecipeTranslation Create(Guid recipeId, string language, string content, string sourceHash) =>
        new()
        {
            RecipeId = recipeId,
            Language = language,
            Content = content,
            SourceHash = sourceHash,
            TranslatedAt = DateTimeOffset.UtcNow
        };
}

public sealed class RecipeCookLog
{
    private RecipeCookLog()
    {
    }

    public Guid Id { get; private set; }
    public Guid RecipeId { get; private set; }
    public string UserId { get; private set; } = string.Empty;
    public DateTimeOffset CookedAt { get; private set; }
    public int? Rating { get; private set; }
    public string? Notes { get; private set; }

    public static RecipeCookLog Create(Guid recipeId, string userId, int? rating, string? notes) =>
        new()
        {
            Id = Guid.NewGuid(),
            RecipeId = recipeId,
            UserId = userId,
            CookedAt = DateTimeOffset.UtcNow,
            Rating = rating,
            Notes = NormalizeNotes(notes)
        };

    public void Update(int? rating, string? notes)
    {
        Rating = rating;
        Notes = NormalizeNotes(notes);
    }

    private static string? NormalizeNotes(string? notes) =>
        string.IsNullOrWhiteSpace(notes) ? null : notes.Trim();
}

public static class TermKinds
{
    public const string MealType = "mealType";
    public const string Course = "course";
    public const string CookingMethod = "cookingMethod";
    public const string Cuisine = "cuisine";
    public const string Tag = "tag";

    public static readonly IReadOnlyList<string> All = [MealType, Course, CookingMethod, Cuisine, Tag];
}

// A premade taxonomy value (meal type, course, cooking method, cuisine or tag).
public sealed class Term
{
    private Term()
    {
    }

    public string Kind { get; private set; } = string.Empty;
    public string Slug { get; private set; } = string.Empty;
    public string NameEn { get; private set; } = string.Empty;
    public string NameDa { get; private set; } = string.Empty;
    public int Position { get; private set; }
}

public sealed class RecipeTerm
{
    private RecipeTerm()
    {
    }

    public Guid RecipeId { get; private set; }
    public string Kind { get; private set; } = string.Empty;
    public string Slug { get; private set; } = string.Empty;

    public static RecipeTerm Create(string kind, string slug) => new() { Kind = kind, Slug = slug };
}

// A supermarket section used to group the shopping list (e.g. dairy, frozen).
public sealed class ShopSection
{
    public const string DefaultSlug = "dry_goods";

    private ShopSection()
    {
    }

    public string Slug { get; private set; } = string.Empty;
    public string NameEn { get; private set; } = string.Empty;
    public string NameDa { get; private set; } = string.Empty;
    public int Position { get; private set; }

    // Perishable sections count as fast-spoiling when suggesting recipes.
    public bool Perishable { get; private set; }

    public string NameFor(string language) =>
        language == "da" && !string.IsNullOrWhiteSpace(NameDa) ? NameDa : NameEn;
}

// A premade ingredient that recipes reference instead of free text.
public sealed class Ingredient
{
    private Ingredient()
    {
    }

    public Guid Id { get; private set; }
    public string NameEn { get; private set; } = string.Empty;
    public string NameDa { get; private set; } = string.Empty;
    public string ShopSectionSlug { get; private set; } = ShopSection.DefaultSlug;

    public string NameFor(string language) =>
        language == "da" && !string.IsNullOrWhiteSpace(NameDa) ? NameDa : NameEn;

    public void Update(string nameEn, string nameDa, string shopSectionSlug)
    {
        NameEn = nameEn.Trim();
        NameDa = nameDa.Trim();
        ShopSectionSlug = shopSectionSlug;
    }

    public static Ingredient Create(string nameEn, string nameDa, string shopSectionSlug) =>
        new()
        {
            Id = Guid.NewGuid(),
            NameEn = nameEn.Trim(),
            NameDa = nameDa.Trim(),
            ShopSectionSlug = shopSectionSlug
        };
}

public sealed class RecipeIngredient
{
    private RecipeIngredient()
    {
    }

    public Guid Id { get; private set; }
    public Guid RecipeId { get; private set; }
    public Guid? IngredientId { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public decimal Quantity { get; private set; }
    public string Unit { get; private set; } = string.Empty;
    public string? Section { get; private set; }
    public string? Notes { get; private set; }
    public int Position { get; private set; }

    public static RecipeIngredient Create(
        Guid ingredientId,
        string name,
        decimal quantity,
        string unit,
        string? section,
        string? notes,
        int position) =>
        new()
        {
            Id = Guid.NewGuid(),
            IngredientId = ingredientId,
            Name = name.Trim(),
            Quantity = quantity,
            Unit = unit.Trim(),
            Section = string.IsNullOrWhiteSpace(section) ? null : section.Trim(),
            Notes = notes?.Trim(),
            Position = position
        };
}

public sealed class RecipeStep
{
    private RecipeStep()
    {
    }

    public Guid Id { get; private set; }
    public Guid RecipeId { get; private set; }
    public string Instructions { get; private set; } = string.Empty;
    public int Position { get; private set; }

    public static RecipeStep Create(string instructions, int position) =>
        new()
        {
            Id = Guid.NewGuid(),
            Instructions = instructions.Trim(),
            Position = position
        };
}

public sealed class RecipeEquipment
{
    private RecipeEquipment()
    {
    }

    public Guid Id { get; private set; }
    public Guid RecipeId { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public int Position { get; private set; }

    public static RecipeEquipment Create(string name, int position) =>
        new()
        {
            Id = Guid.NewGuid(),
            Name = name.Trim(),
            Position = position
        };
}
