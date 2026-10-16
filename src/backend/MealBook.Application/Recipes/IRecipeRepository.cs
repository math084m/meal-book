using MealBook.Domain.Recipes;

namespace MealBook.Application.Recipes;

public interface IRecipeRepository
{
    Task<IReadOnlyList<RecipeView>> GetAllAsync(
        RecipeSearchRequest search,
        CancellationToken cancellationToken);

    Task<IReadOnlyList<RecipeView>> GetDraftsAsync(CancellationToken cancellationToken);

    Task<RecipeView?> GetByIdAsync(Guid id, CancellationToken cancellationToken);

    Task AddAsync(Recipe recipe, CancellationToken cancellationToken);

    Task<bool> UpdateAsync(Recipe recipe, CancellationToken cancellationToken);

    Task<IReadOnlyDictionary<Guid, RecipeTranslation>> GetTranslationsAsync(
        IReadOnlyCollection<Guid> recipeIds,
        string language,
        CancellationToken cancellationToken);

    Task SaveTranslationAsync(RecipeTranslation translation, CancellationToken cancellationToken);

    Task<bool> PublishAsync(Guid id, CancellationToken cancellationToken);

    Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken);

    Task<RecipeImageInfo?> GetImageInfoAsync(Guid id, CancellationToken cancellationToken);

    Task<bool> SetImageAsync(Guid id, DateTimeOffset? updatedAt, CancellationToken cancellationToken);

    Task<RecipeCookedResponse?> LogCookedAsync(
        Guid recipeId,
        string userId,
        LogRecipeCookedRequest request,
        CancellationToken cancellationToken);

    Task<IReadOnlyList<RecipeCookedResponse>> GetCookHistoryAsync(
        string userId,
        CancellationToken cancellationToken);

    Task<RecipeDashboardData> GetDashboardAsync(CancellationToken cancellationToken);
}

public sealed record RecipeView(
    Recipe Recipe,
    int CookCount,
    double? AverageRating,
    int RecentCookCount = 0);
