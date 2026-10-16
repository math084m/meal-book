namespace MealBook.Application.Recipes;

public interface IRecipeService
{
    Task<IReadOnlyList<RecipeResponse>> GetAllAsync(
        RecipeSearchRequest search,
        CancellationToken cancellationToken);

    Task<RecipeResponse?> GetByIdAsync(Guid id, CancellationToken cancellationToken);

    Task<RecipeDashboardResponse> GetDashboardAsync(CancellationToken cancellationToken);

    Task<RecipeResponse> CreateAsync(
        CreateRecipeRequest request,
        CancellationToken cancellationToken);

    Task<RecipeResponse> ImportAsync(
        ImportRecipeRequest request,
        CancellationToken cancellationToken);

    Task<IReadOnlyList<RecipeResponse>> GetDraftsAsync(CancellationToken cancellationToken);

    Task<RecipeResponse?> UpdateAsync(
        Guid id,
        UpdateRecipeRequest request,
        CancellationToken cancellationToken);

    Task<RecipeResponse?> PublishAsync(Guid id, CancellationToken cancellationToken);

    Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken);

    Task<RecipeCookedResponse?> LogCookedAsync(
        Guid recipeId,
        string userId,
        LogRecipeCookedRequest request,
        CancellationToken cancellationToken);

    Task<IReadOnlyList<RecipeCookedResponse>> GetCookHistoryAsync(
        string userId,
        CancellationToken cancellationToken);
}
