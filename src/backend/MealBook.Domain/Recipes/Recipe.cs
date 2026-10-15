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
    public string MealType { get; private set; } = string.Empty;
    public string Course { get; private set; } = string.Empty;
    public string CookingMethod { get; private set; } = string.Empty;
    public string Cuisine { get; private set; } = string.Empty;
    public int? CaloriesPerServing { get; private set; }
    public int? ProteinGramsPerServing { get; private set; }
    public int? CarbohydratesGramsPerServing { get; private set; }
    public int? FatGramsPerServing { get; private set; }
    public DateTimeOffset AddedAt { get; private set; }
    public bool IsPublished { get; private set; }
    public string? SourceUrl { get; private set; }
    public List<RecipeIngredient> Ingredients { get; private set; } = [];
    public List<RecipeStep> Steps { get; private set; } = [];
    public List<RecipeEquipment> Equipment { get; private set; } = [];
    public List<RecipeTag> Tags { get; private set; } = [];

    public static Recipe Create(
        string name,
        string? description,
        int servings,
        int prepTimeMinutes,
        int cookTimeMinutes,
        string mealType,
        string course,
        string cookingMethod,
        string cuisine,
        int? caloriesPerServing,
        int? proteinGramsPerServing,
        int? carbohydratesGramsPerServing,
        int? fatGramsPerServing,
        IEnumerable<RecipeIngredient> ingredients,
        IEnumerable<RecipeStep> steps,
        IEnumerable<RecipeEquipment> equipment,
        IEnumerable<string> tags,
        Guid? id = null,
        DateTimeOffset? addedAt = null,
        bool isPublished = true,
        string? sourceUrl = null) =>
        new()
        {
            Id = id ?? Guid.NewGuid(),
            Name = name.Trim(),
            Description = description?.Trim(),
            Servings = servings,
            PrepTimeMinutes = prepTimeMinutes,
            CookTimeMinutes = cookTimeMinutes,
            MealType = mealType.Trim(),
            Course = course.Trim(),
            CookingMethod = cookingMethod.Trim(),
            Cuisine = cuisine.Trim(),
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
            Tags = tags
                .Select(tag => tag.Trim())
                .Where(tag => tag.Length > 0)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .Select((tag, position) => RecipeTag.Create(tag, position))
                .ToList()
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
            Notes = notes?.Trim()
        };
}

public sealed class RecipeTag
{
    private RecipeTag()
    {
    }

    public Guid Id { get; private set; }
    public Guid RecipeId { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public int Position { get; private set; }

    public static RecipeTag Create(string name, int position) =>
        new() { Id = Guid.NewGuid(), Name = name.Trim(), Position = position };
}

public sealed class RecipeIngredient
{
    private RecipeIngredient()
    {
    }

    public Guid Id { get; private set; }
    public Guid RecipeId { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public decimal Quantity { get; private set; }
    public string Unit { get; private set; } = string.Empty;
    public string? Section { get; private set; }
    public string? Notes { get; private set; }
    public int Position { get; private set; }

    public static RecipeIngredient Create(
        string name,
        decimal quantity,
        string unit,
        string? section,
        string? notes,
        int position) =>
        new()
        {
            Id = Guid.NewGuid(),
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
