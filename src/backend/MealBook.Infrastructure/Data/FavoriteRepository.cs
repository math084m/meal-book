using MealBook.Application.Favorites;
using MealBook.Domain.Favorites;
using Microsoft.EntityFrameworkCore;

namespace MealBook.Infrastructure.Data;

public sealed class FavoriteRepository(RecipeDbContext db) : IFavoriteRepository
{
    public async Task<IReadOnlyList<Guid>> GetRecipeIdsAsync(string userId, CancellationToken cancellationToken) =>
        await db.Set<RecipeFavorite>()
            .AsNoTracking()
            .Where(favorite => favorite.UserId == userId)
            .OrderByDescending(favorite => favorite.CreatedAt)
            .Select(favorite => favorite.RecipeId)
            .ToListAsync(cancellationToken);

    public async Task<bool> AddAsync(string userId, Guid recipeId, CancellationToken cancellationToken)
    {
        if (!await db.Recipes.AnyAsync(recipe => recipe.Id == recipeId && recipe.IsPublished, cancellationToken))
        {
            return false;
        }

        await db.Database.ExecuteSqlInterpolatedAsync(
            $"""
            INSERT INTO "RecipeFavorites" ("UserId", "RecipeId", "CreatedAt")
            VALUES ({userId}, {recipeId}, {DateTimeOffset.UtcNow})
            ON CONFLICT DO NOTHING
            """,
            cancellationToken);
        return true;
    }

    public async Task RemoveAsync(string userId, Guid recipeId, CancellationToken cancellationToken) =>
        await db.Set<RecipeFavorite>()
            .Where(favorite => favorite.UserId == userId && favorite.RecipeId == recipeId)
            .ExecuteDeleteAsync(cancellationToken);
}
