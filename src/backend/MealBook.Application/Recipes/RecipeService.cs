using MealBook.Domain.Recipes;

namespace MealBook.Application.Recipes;

public sealed class RecipeService(IRecipeRepository repository) : IRecipeService
{
    public async Task<IReadOnlyList<RecipeResponse>> GetAllAsync(
        RecipeSearchRequest search,
        CancellationToken cancellationToken)
    {
        var recipes = await repository.GetAllAsync(search, cancellationToken);
        return recipes.Select(ToResponse).ToArray();
    }

    public async Task<RecipeResponse?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        var recipe = await repository.GetByIdAsync(id, cancellationToken);
        return recipe is null ? null : ToResponse(recipe);
    }

    public async Task<RecipeDashboardResponse> GetDashboardAsync(CancellationToken cancellationToken)
    {
        var dashboard = await repository.GetDashboardAsync(cancellationToken);
        return new RecipeDashboardResponse(
            dashboard.RecentlyAdded.Select(ToResponse).ToArray(),
            dashboard.MostCooked.Select(ToResponse).ToArray(),
            dashboard.RecentlyCooked
                .Select(item => new RecentlyCookedRecipeResponse(
                    ToResponse(item.Recipe),
                    item.CookedAt))
                .ToArray());
    }

    public async Task<RecipeResponse> CreateAsync(
        CreateRecipeRequest request,
        CancellationToken cancellationToken)
    {
        var recipe = CreateRecipe(request);
        await repository.AddAsync(recipe, cancellationToken);
        return ToResponse(new RecipeView(recipe, 0, null));
    }

    public async Task<RecipeResponse> ImportAsync(
        ImportRecipeRequest request,
        CancellationToken cancellationToken)
    {
        var recipe = CreateRecipe(request, isPublished: false, sourceUrl: request.SourceUrl);
        await repository.AddAsync(recipe, cancellationToken);
        return ToResponse(new RecipeView(recipe, 0, null));
    }

    public async Task<IReadOnlyList<RecipeResponse>> GetDraftsAsync(CancellationToken cancellationToken)
    {
        var drafts = await repository.GetDraftsAsync(cancellationToken);
        return drafts.Select(ToResponse).ToArray();
    }

    public async Task<RecipeResponse?> UpdateAsync(
        Guid id,
        UpdateRecipeRequest request,
        CancellationToken cancellationToken)
    {
        var current = await repository.GetByIdAsync(id, cancellationToken);
        if (current is null)
        {
            return null;
        }

        var recipe = CreateRecipe(request, id, current.Recipe.AddedAt);
        if (!await repository.UpdateAsync(recipe, cancellationToken))
        {
            return null;
        }

        return await GetByIdAsync(id, cancellationToken);
    }

    public async Task<RecipeResponse?> PublishAsync(Guid id, CancellationToken cancellationToken) =>
        await repository.PublishAsync(id, cancellationToken)
            ? await GetByIdAsync(id, cancellationToken)
            : null;

    public Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken) =>
        repository.DeleteAsync(id, cancellationToken);

    public Task<RecipeCookedResponse?> LogCookedAsync(
        Guid recipeId,
        string userId,
        LogRecipeCookedRequest request,
        CancellationToken cancellationToken) =>
        repository.LogCookedAsync(recipeId, userId, request, cancellationToken);

    public Task<IReadOnlyList<RecipeCookedResponse>> GetCookHistoryAsync(
        string userId,
        CancellationToken cancellationToken) =>
        repository.GetCookHistoryAsync(userId, cancellationToken);

    private static Recipe CreateRecipe(
        RecipeWriteRequest request,
        Guid? id = null,
        DateTimeOffset? addedAt = null,
        bool? isPublished = null,
        string? sourceUrl = null) =>
        Recipe.Create(
            request.Name,
            request.Description,
            request.Servings,
            request.PrepTimeMinutes,
            request.CookTimeMinutes,
            request.MealType,
            request.Course,
            request.CookingMethod,
            request.Cuisine,
            request.CaloriesPerServing,
            request.ProteinGramsPerServing,
            request.CarbohydratesGramsPerServing,
            request.FatGramsPerServing,
            request.Ingredients.Select((ingredient, position) =>
                RecipeIngredient.Create(
                    ingredient.Name,
                    ingredient.Quantity,
                    ingredient.Unit,
                    ingredient.Section,
                    ingredient.Notes,
                    position)),
            request.Steps.Select((step, position) => RecipeStep.Create(step, position)),
            request.Equipment.Select((item, position) => RecipeEquipment.Create(item, position)),
            request.Tags,
            id,
            addedAt,
            isPublished ?? request.IsPublished,
            sourceUrl ?? request.SourceUrl);

    private static RecipeResponse ToResponse(RecipeView view)
    {
        var recipe = view.Recipe;
        return new RecipeResponse(
            recipe.Id,
            recipe.Name,
            recipe.Description,
            recipe.Servings,
            recipe.PrepTimeMinutes,
            recipe.CookTimeMinutes,
            recipe.MealType,
            recipe.Course,
            recipe.CookingMethod,
            recipe.Cuisine,
            recipe.CaloriesPerServing,
            recipe.ProteinGramsPerServing,
            recipe.CarbohydratesGramsPerServing,
            recipe.FatGramsPerServing,
            recipe.AddedAt,
            recipe.IsPublished,
            recipe.SourceUrl,
            view.CookCount,
            view.RecentCookCount,
            view.AverageRating,
            recipe.Tags.OrderBy(tag => tag.Position).Select(tag => tag.Name).ToArray(),
            recipe.Ingredients
                .OrderBy(ingredient => ingredient.Position)
                .Select(ingredient => new RecipeIngredientResponse(
                    ingredient.Name,
                    ingredient.Quantity,
                    ingredient.Unit,
                    ingredient.Section,
                    ingredient.Notes))
                .ToArray(),
            recipe.Steps
                .OrderBy(step => step.Position)
                .Select(step => step.Instructions)
                .ToArray(),
            recipe.Equipment
                .OrderBy(item => item.Position)
                .Select(item => item.Name)
                .ToArray());
    }
}
