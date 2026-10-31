using MealBook.Application.Pantry;
using MealBook.Domain.Pantry;
using Microsoft.EntityFrameworkCore;

namespace MealBook.Infrastructure.Data;

public sealed class PantryRepository(RecipeDbContext db) : IPantryRepository
{
    public async Task<PantryResponse> GetAsync(string userId, CancellationToken cancellationToken)
    {
        var enabled = await db.Users.AsNoTracking()
            .Where(user => user.Id == userId)
            .Select(user => user.UsePantryForShopping)
            .SingleAsync(cancellationToken);
        var items = await db.Set<PantryItem>().AsNoTracking()
            .Where(item => item.UserId == userId)
            .OrderBy(item => item.IngredientId)
            .Select(item => new PantryItemResponse(item.IngredientId, item.HaveFraction))
            .ToListAsync(cancellationToken);
        return new PantryResponse(enabled, items);
    }

    public async Task SetUseForShoppingAsync(string userId, bool enabled, CancellationToken cancellationToken) =>
        await db.Users.Where(user => user.Id == userId)
            .ExecuteUpdateAsync(update => update.SetProperty(user => user.UsePantryForShopping, enabled), cancellationToken);

    public async Task<bool> AddAsync(string userId, Guid ingredientId, CancellationToken cancellationToken)
    {
        var affected = await db.Database.ExecuteSqlInterpolatedAsync(
            $"""
            INSERT INTO "PantryItems" ("UserId", "IngredientId")
            SELECT {userId}, {ingredientId}
            WHERE EXISTS (SELECT 1 FROM "Ingredients" WHERE "Id" = {ingredientId})
            ON CONFLICT ("UserId", "IngredientId")
            DO UPDATE SET "IngredientId" = EXCLUDED."IngredientId"
            """,
            cancellationToken);
        return affected > 0;
    }

    public async Task RemoveAsync(string userId, Guid ingredientId, CancellationToken cancellationToken) =>
        await db.Set<PantryItem>()
            .Where(item => item.UserId == userId && item.IngredientId == ingredientId)
            .ExecuteDeleteAsync(cancellationToken);

    public async Task<bool> SetHaveFractionAsync(
        string userId, Guid ingredientId, decimal? fraction, CancellationToken cancellationToken) =>
        await db.Set<PantryItem>()
            .Where(item => item.UserId == userId && item.IngredientId == ingredientId)
            .ExecuteUpdateAsync(update => update.SetProperty(item => item.HaveFraction, fraction), cancellationToken) > 0;
}
