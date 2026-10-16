namespace MealBook.Domain.Planning;

// A recipe the household plans to cook, with the number of servings to shop for.
public sealed class MealPlanEntry
{
    public const int MaxServings = 100;

    private MealPlanEntry()
    {
    }

    public Guid Id { get; private set; }
    public Guid RecipeId { get; private set; }
    public int Servings { get; private set; }
    public string AddedByUserId { get; private set; } = string.Empty;
    public DateTimeOffset AddedAt { get; private set; }

    public void SetServings(int servings) => Servings = Math.Clamp(servings, 1, MaxServings);

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
