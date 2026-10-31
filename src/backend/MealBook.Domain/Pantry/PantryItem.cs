namespace MealBook.Domain.Pantry;

public sealed class PantryItem
{
    private PantryItem()
    {
    }

    public string UserId { get; private set; } = string.Empty;
    public Guid IngredientId { get; private set; }

    // Null uses the pantry default; zero explicitly means this user needs to buy it.
    public decimal? HaveFraction { get; private set; }
}
