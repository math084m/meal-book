namespace MealBook.Application.Favorites;

public interface IFavoriteRepository
{
    Task<IReadOnlyList<Guid>> GetRecipeIdsAsync(string userId, CancellationToken cancellationToken);

    // False when the recipe does not exist or is an unpublished draft.
    Task<bool> AddAsync(string userId, Guid recipeId, CancellationToken cancellationToken);

    Task RemoveAsync(string userId, Guid recipeId, CancellationToken cancellationToken);
}
