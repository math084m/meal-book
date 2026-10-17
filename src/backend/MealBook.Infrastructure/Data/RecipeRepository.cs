using MealBook.Application.Recipes;
using MealBook.Domain.Recipes;
using Microsoft.EntityFrameworkCore;

namespace MealBook.Infrastructure.Data;

public sealed class RecipeRepository(RecipeDbContext db) : IRecipeRepository
{
    public async Task<IReadOnlyList<RecipeView>> GetAllAsync(
        RecipeSearchRequest search,
        CancellationToken cancellationToken)
    {
        var query = CreateRecipeQuery().Where(recipe => recipe.IsPublished);
        var term = search.Search?.Trim();
        if (!string.IsNullOrEmpty(term))
        {
            var pattern = $"%{term.Replace("\\", "\\\\").Replace("%", "\\%").Replace("_", "\\_")}%";
            query = query.Where(recipe =>
                EF.Functions.ILike(recipe.Name, pattern)
                || (recipe.Description != null && EF.Functions.ILike(recipe.Description, pattern))
                || recipe.Ingredients.Any(item => db.Set<Ingredient>().Any(ingredient =>
                    ingredient.Id == item.IngredientId
                    && (EF.Functions.ILike(ingredient.NameEn, pattern)
                        || EF.Functions.ILike(ingredient.NameDa, pattern)))));
        }

        if (search.MaxTotalMinutes is { } maxMinutes)
        {
            query = query.Where(recipe => recipe.PrepTimeMinutes + recipe.CookTimeMinutes <= maxMinutes);
        }

        if (search.MinTotalMinutes is { } minMinutes)
        {
            query = query.Where(recipe => recipe.PrepTimeMinutes + recipe.CookTimeMinutes >= minMinutes);
        }

        query = FilterByTerms(query, TermKinds.MealType, search.MealTypes);
        query = FilterByTerms(query, TermKinds.Course, search.Courses);
        query = FilterByTerms(query, TermKinds.CookingMethod, search.CookingMethods);
        query = FilterByTerms(query, TermKinds.Cuisine, search.Cuisines);
        query = FilterByTerms(query, TermKinds.Tag, search.Tags);

        foreach (var ingredientId in search.Ingredients.Distinct())
        {
            query = query.Where(recipe => recipe.Ingredients.Any(item => item.IngredientId == ingredientId));
        }

        if (search.MaxCalories is { } maxCalories)
        {
            query = query.Where(recipe =>
                recipe.CaloriesPerServing != null && recipe.CaloriesPerServing <= maxCalories);
        }

        var recipes = await query.ToListAsync(cancellationToken);
        var views = await AddStatisticsAsync(recipes, cancellationToken);

        if (search.MinRating is { } minRating)
        {
            views = views.Where(view => view.AverageRating >= minRating).ToList();
        }

        return search.Sort.ToLowerInvariant() switch
        {
            "popular" => views.OrderByDescending(view => view.CookCount).ThenBy(view => view.Recipe.Name).ToArray(),
            "trending" => views.OrderByDescending(view => view.RecentCookCount)
                .ThenByDescending(view => view.CookCount)
                .ToArray(),
            "rating" => views.OrderByDescending(view => view.AverageRating ?? 0)
                .ThenBy(view => view.Recipe.Name).ToArray(),
            "quickest" => views.OrderBy(view => view.Recipe.PrepTimeMinutes + view.Recipe.CookTimeMinutes)
                .ThenBy(view => view.Recipe.Name).ToArray(),
            "name" => views.OrderBy(view => view.Recipe.Name).ToArray(),
            _ => views.OrderByDescending(view => view.Recipe.AddedAt).ToArray()
        };
    }

    public async Task<IReadOnlyList<RecipeView>> GetDraftsAsync(CancellationToken cancellationToken)
    {
        var drafts = await CreateRecipeQuery()
            .Where(recipe => !recipe.IsPublished)
            .OrderByDescending(recipe => recipe.AddedAt)
            .ToListAsync(cancellationToken);
        return await AddStatisticsAsync(drafts, cancellationToken);
    }

    public async Task<RecipeView?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        var recipe = await CreateRecipeQuery()
            .SingleOrDefaultAsync(item => item.Id == id, cancellationToken);
        if (recipe is null)
        {
            return null;
        }

        return (await AddStatisticsAsync([recipe], cancellationToken))[0];
    }

    public async Task AddAsync(Recipe recipe, CancellationToken cancellationToken)
    {
        db.Recipes.Add(recipe);
        await db.SaveChangesAsync(cancellationToken);
    }

    public async Task<bool> UpdateAsync(Recipe recipe, CancellationToken cancellationToken)
    {
        var existing = await db.Recipes
            .AsSplitQuery()
            .Include(item => item.Ingredients)
            .Include(item => item.Steps)
            .Include(item => item.Equipment)
            .Include(item => item.Terms)
            .SingleOrDefaultAsync(item => item.Id == recipe.Id, cancellationToken);
        if (existing is null)
        {
            return false;
        }

        var imageUpdatedAt = existing.ImageUpdatedAt;
        db.Entry(existing).CurrentValues.SetValues(recipe);
        existing.SetImage(imageUpdatedAt);
        ReplaceChildren(existing.Ingredients, recipe.Ingredients);
        ReplaceChildren(existing.Steps, recipe.Steps);
        ReplaceChildren(existing.Equipment, recipe.Equipment);

        // Terms have natural keys, so diff them instead of replacing to avoid key conflicts.
        var wanted = recipe.Terms.Select(term => (term.Kind, term.Slug)).ToHashSet();
        foreach (var term in existing.Terms.Where(term => !wanted.Contains((term.Kind, term.Slug))).ToList())
        {
            existing.Terms.Remove(term);
            db.Remove(term);
        }

        var current = existing.Terms.Select(term => (term.Kind, term.Slug)).ToHashSet();
        foreach (var term in recipe.Terms.Where(term => !current.Contains((term.Kind, term.Slug))))
        {
            db.Entry(term).State = EntityState.Added;
            existing.Terms.Add(term);
        }

        await db.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<IReadOnlyDictionary<Guid, RecipeTranslation>> GetTranslationsAsync(
        IReadOnlyCollection<Guid> recipeIds,
        string language,
        CancellationToken cancellationToken)
    {
        if (recipeIds.Count == 0)
        {
            return new Dictionary<Guid, RecipeTranslation>();
        }

        var ids = recipeIds.ToArray();
        return await db.Set<RecipeTranslation>()
            .AsNoTracking()
            .Where(item => item.Language == language && ids.Contains(item.RecipeId))
            .ToDictionaryAsync(item => item.RecipeId, cancellationToken);
    }

    public async Task SaveTranslationAsync(RecipeTranslation translation, CancellationToken cancellationToken)
    {
        var existing = await db.Set<RecipeTranslation>()
            .SingleOrDefaultAsync(
                item => item.RecipeId == translation.RecipeId && item.Language == translation.Language,
                cancellationToken);
        if (existing is not null)
        {
            db.Remove(existing);
        }

        db.Add(translation);
        await db.SaveChangesAsync(cancellationToken);
    }

    public async Task<bool> PublishAsync(Guid id, CancellationToken cancellationToken)
    {
        var recipe = await db.Recipes.SingleOrDefaultAsync(item => item.Id == id, cancellationToken);
        if (recipe is null)
        {
            return false;
        }

        recipe.Publish();
        await db.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<RecipeImageInfo?> GetImageInfoAsync(Guid id, CancellationToken cancellationToken) =>
        await db.Recipes
            .AsNoTracking()
            .Where(item => item.Id == id)
            .Select(item => new RecipeImageInfo(item.IsPublished, item.ImageUpdatedAt))
            .SingleOrDefaultAsync(cancellationToken);

    public async Task<bool> SetImageAsync(Guid id, DateTimeOffset? updatedAt, CancellationToken cancellationToken)
    {
        var recipe = await db.Recipes.SingleOrDefaultAsync(item => item.Id == id, cancellationToken);
        if (recipe is null)
        {
            return false;
        }

        recipe.SetImage(updatedAt);
        await db.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken)
    {
        var recipe = await db.Recipes.SingleOrDefaultAsync(item => item.Id == id, cancellationToken);
        if (recipe is null)
        {
            return false;
        }

        db.Recipes.Remove(recipe);
        await db.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<RecipeCookedResponse?> LogCookedAsync(
        Guid recipeId,
        string userId,
        LogRecipeCookedRequest request,
        CancellationToken cancellationToken)
    {
        var recipeName = await db.Recipes
            .Where(recipe => recipe.Id == recipeId && recipe.IsPublished)
            .Select(recipe => recipe.Name)
            .SingleOrDefaultAsync(cancellationToken);
        if (recipeName is null)
        {
            return null;
        }

        var log = RecipeCookLog.Create(recipeId, userId, request.Rating, request.Notes);
        db.RecipeCookLogs.Add(log);
        await db.SaveChangesAsync(cancellationToken);
        return new RecipeCookedResponse(
            log.Id,
            log.RecipeId,
            recipeName,
            log.CookedAt,
            log.Rating,
            log.Notes);
    }

    public async Task<IReadOnlyList<RecipeCookedResponse>> GetCookHistoryAsync(
        string userId,
        Guid? recipeId,
        CancellationToken cancellationToken) =>
        await (
            from log in db.RecipeCookLogs.AsNoTracking()
            join recipe in db.Recipes.AsNoTracking() on log.RecipeId equals recipe.Id
            where log.UserId == userId && (recipeId == null || log.RecipeId == recipeId)
            orderby log.CookedAt descending
            select new RecipeCookedResponse(
                log.Id,
                log.RecipeId,
                recipe.Name,
                log.CookedAt,
                log.Rating,
                log.Notes))
            .ToListAsync(cancellationToken);

    public async Task<RecipeCookedResponse?> UpdateCookLogAsync(
        Guid logId,
        string userId,
        LogRecipeCookedRequest request,
        CancellationToken cancellationToken)
    {
        var log = await db.RecipeCookLogs
            .SingleOrDefaultAsync(item => item.Id == logId && item.UserId == userId, cancellationToken);
        if (log is null)
        {
            return null;
        }

        log.Update(request.Rating, request.Notes);
        await db.SaveChangesAsync(cancellationToken);
        var recipeName = await db.Recipes
            .Where(recipe => recipe.Id == log.RecipeId)
            .Select(recipe => recipe.Name)
            .SingleAsync(cancellationToken);
        return new RecipeCookedResponse(log.Id, log.RecipeId, recipeName, log.CookedAt, log.Rating, log.Notes);
    }

    public async Task<bool> DeleteCookLogAsync(Guid logId, string userId, CancellationToken cancellationToken) =>
        await db.RecipeCookLogs
            .Where(item => item.Id == logId && item.UserId == userId)
            .ExecuteDeleteAsync(cancellationToken) > 0;

    public async Task<RecipeDashboardData> GetDashboardAsync(CancellationToken cancellationToken)
    {
        var recipes = await CreateRecipeQuery()
            .Where(recipe => recipe.IsPublished)
            .ToListAsync(cancellationToken);
        var views = await AddStatisticsAsync(recipes, cancellationToken);
        var recentLogs = await db.RecipeCookLogs
            .AsNoTracking()
            .OrderByDescending(log => log.CookedAt)
            .Take(6)
            .Select(log => new { log.RecipeId, log.UserId, log.CookedAt })
            .ToListAsync(cancellationToken);
        var recentRecipeIds = recentLogs.Select(log => log.RecipeId).Distinct().ToArray();
        var recentRecipes = views.Where(view => recentRecipeIds.Contains(view.Recipe.Id))
            .ToDictionary(view => view.Recipe.Id);
        return new RecipeDashboardData(
            views.OrderByDescending(view => view.Recipe.AddedAt).Take(6).ToArray(),
            views.OrderByDescending(view => view.CookCount).ThenBy(view => view.Recipe.Name).Take(6).ToArray(),
            recentLogs
                .Where(log => recentRecipes.ContainsKey(log.RecipeId))
                .Select(log => new RecentlyCookedRecipeData(
                    recentRecipes[log.RecipeId],
                    log.CookedAt))
                .ToArray());
    }

    private async Task<List<RecipeView>> AddStatisticsAsync(
        IReadOnlyCollection<Recipe> recipes,
        CancellationToken cancellationToken)
    {
        if (recipes.Count == 0)
        {
            return [];
        }

        var ids = recipes.Select(recipe => recipe.Id).ToArray();
        var recentThreshold = DateTimeOffset.UtcNow.AddDays(-30);
        var statistics = await db.RecipeCookLogs
            .AsNoTracking()
            .Where(log => ids.Contains(log.RecipeId))
            .GroupBy(log => log.RecipeId)
            .Select(group => new
            {
                RecipeId = group.Key,
                CookCount = group.Count(),
                RecentCookCount = group.Count(log => log.CookedAt >= recentThreshold),
                AverageRating = group.Where(log => log.Rating != null)
                    .Average(log => (double?)log.Rating)
            })
            .ToDictionaryAsync(item => item.RecipeId, cancellationToken);

        return recipes.Select(recipe =>
        {
            statistics.TryGetValue(recipe.Id, out var values);
            return new RecipeView(
                recipe,
                values?.CookCount ?? 0,
                values?.AverageRating,
                values?.RecentCookCount ?? 0);
        }).ToList();
    }

    private static IQueryable<Recipe> FilterByTerms(
        IQueryable<Recipe> query,
        string kind,
        IReadOnlyCollection<string> slugs)
    {
        var values = slugs.Where(slug => !string.IsNullOrWhiteSpace(slug)).Distinct().ToArray();
        return values.Length == 0
            ? query
            : query.Where(recipe => recipe.Terms.Any(term => term.Kind == kind && values.Contains(term.Slug)));
    }

    private IQueryable<Recipe> CreateRecipeQuery() =>
        db.Recipes
            .AsNoTracking()
            .AsSplitQuery()
            .Include(recipe => recipe.Ingredients)
            .Include(recipe => recipe.Steps)
            .Include(recipe => recipe.Equipment)
            .Include(recipe => recipe.Terms);

    private void ReplaceChildren<T>(List<T> current, List<T> replacement)
        where T : class
    {
        current.Clear();
        current.AddRange(replacement);
        foreach (var item in replacement)
        {
            db.Entry(item).State = EntityState.Added;
        }
    }
}
