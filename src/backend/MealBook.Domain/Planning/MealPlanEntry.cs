namespace MealBook.Domain.Planning;

// A recipe the household plans to cook, with the number of servings to shop for.
public sealed class MealPlanEntry
{
    public const int MaxServings = 100;
    public const int MaxPlannedDays = 14;

    private MealPlanEntry()
    {
    }

    public Guid Id { get; private set; }
    public Guid RecipeId { get; private set; }
    public int Servings { get; private set; }
    public string AddedByUserId { get; private set; } = string.Empty;
    public DateTimeOffset AddedAt { get; private set; }

    // The first day the recipe is cooked or eaten, and how many days it covers (leftovers). Null means unscheduled.
    public DateOnly? PlannedStart { get; private set; }
    public int PlannedDays { get; private set; } = 1;

    public void SetServings(int servings) => Servings = Math.Clamp(servings, 1, MaxServings);

    public void Schedule(DateOnly? start, int days)
    {
        PlannedStart = start;
        PlannedDays = start is null ? 1 : Math.Clamp(days, 1, MaxPlannedDays);
    }

    public static MealPlanEntry Create(Guid recipeId, int servings, string userId) =>
        new()
        {
            Id = Guid.NewGuid(),
            RecipeId = recipeId,
            Servings = Math.Clamp(servings, 1, MaxServings),
            AddedByUserId = userId,
            AddedAt = DateTimeOffset.UtcNow
        };
}

// An item added by hand (toilet paper, batteries) that belongs to no recipe.
public sealed class ShoppingCustomItem
{
    public const int MaxNameLength = 120;
    public const int MaxUnitLength = 40;

    private ShoppingCustomItem()
    {
    }

    public Guid Id { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public decimal Quantity { get; private set; }
    public string Unit { get; private set; } = string.Empty;
    public string SectionSlug { get; private set; } = string.Empty;
    public bool IsChecked { get; private set; }
    public string AddedByUserId { get; private set; } = string.Empty;
    public DateTimeOffset AddedAt { get; private set; }

    public static ShoppingCustomItem Create(
        string name,
        decimal quantity,
        string unit,
        string sectionSlug,
        string userId) =>
        new()
        {
            Id = Guid.NewGuid(),
            Name = name.Trim(),
            Quantity = Math.Max(quantity, 0),
            Unit = unit.Trim(),
            SectionSlug = sectionSlug,
            AddedByUserId = userId,
            AddedAt = DateTimeOffset.UtcNow
        };
}

// An amount of an ingredient already at home, in the normalised aggregation unit.
public sealed class ShoppingHave
{
    private ShoppingHave()
    {
    }

    public Guid IngredientId { get; private set; }
    public string Unit { get; private set; } = string.Empty;
    public decimal Quantity { get; private set; }
    public string UpdatedByUserId { get; private set; } = string.Empty;
    public DateTimeOffset UpdatedAt { get; private set; }
}

// A shopping list line that has been ticked off. The unit is the normalised aggregation unit.
public sealed class ShoppingCheck
{
    private ShoppingCheck()
    {
    }

    public Guid IngredientId { get; private set; }
    public string Unit { get; private set; } = string.Empty;
    public string CheckedByUserId { get; private set; } = string.Empty;
    public DateTimeOffset CheckedAt { get; private set; }

    public static ShoppingCheck Create(Guid ingredientId, string unit, string userId) =>
        new()
        {
            IngredientId = ingredientId,
            Unit = unit,
            CheckedByUserId = userId,
            CheckedAt = DateTimeOffset.UtcNow
        };
}
