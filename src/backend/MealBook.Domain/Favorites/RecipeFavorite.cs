namespace MealBook.Domain.Favorites;

// A recipe a user has starred. Favourites are personal, unlike the shared meal plan.
public sealed class RecipeFavorite
{
    private RecipeFavorite()
    {
    }

    public string UserId { get; private set; } = string.Empty;
    public Guid RecipeId { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }

    public static RecipeFavorite Create(string userId, Guid recipeId) =>
        new() { UserId = userId, RecipeId = recipeId, CreatedAt = DateTimeOffset.UtcNow };
}
