using MealBook.Application.Recipes;
using MealBook.Domain.Recipes;
using Microsoft.EntityFrameworkCore;

namespace MealBook.Infrastructure.Data;

public sealed class CatalogRepository(RecipeDbContext db) : ICatalogRepository
{
    public async Task<IReadOnlyList<Term>> GetTermsAsync(CancellationToken cancellationToken) =>
        await db.Set<Term>()
            .AsNoTracking()
            .OrderBy(term => term.Kind)
            .ThenBy(term => term.Position)
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<Ingredient>> GetIngredientsAsync(CancellationToken cancellationToken) =>
        await db.Set<Ingredient>()
            .AsNoTracking()
            .OrderBy(item => item.NameEn)
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<ShopSection>> GetShopSectionsAsync(CancellationToken cancellationToken) =>
        await db.Set<ShopSection>()
            .AsNoTracking()
            .OrderBy(section => section.Position)
            .ToListAsync(cancellationToken);

    public Task<bool> IngredientNameExistsAsync(
        string nameEn,
        Guid? exceptId,
        CancellationToken cancellationToken)
    {
        var name = nameEn.Trim().ToLower();
        return db.Set<Ingredient>()
            .AnyAsync(item => item.NameEn.ToLower() == name && item.Id != exceptId, cancellationToken);
    }

    public async Task<Ingredient> AddIngredientAsync(Ingredient ingredient, CancellationToken cancellationToken)
    {
        db.Set<Ingredient>().Add(ingredient);
        await db.SaveChangesAsync(cancellationToken);
        return ingredient;
    }

    public async Task<Ingredient?> UpdateIngredientAsync(
        Guid id,
        string nameEn,
        string nameDa,
        string shopSectionSlug,
        CancellationToken cancellationToken)
    {
        var ingredient = await db.Set<Ingredient>().SingleOrDefaultAsync(item => item.Id == id, cancellationToken);
        if (ingredient is null)
        {
            return null;
        }

        ingredient.Update(nameEn, nameDa, shopSectionSlug);
        await db.SaveChangesAsync(cancellationToken);
        return ingredient;
    }

    public async Task<IngredientDeleteResult> DeleteIngredientAsync(Guid id, CancellationToken cancellationToken)
    {
        var ingredient = await db.Set<Ingredient>().SingleOrDefaultAsync(item => item.Id == id, cancellationToken);
        if (ingredient is null)
        {
            return IngredientDeleteResult.NotFound;
        }

        if (await db.Set<RecipeIngredient>().AnyAsync(item => item.IngredientId == id, cancellationToken))
        {
            return IngredientDeleteResult.InUse;
        }

        db.Remove(ingredient);
        await db.SaveChangesAsync(cancellationToken);
        return IngredientDeleteResult.Deleted;
    }
}
