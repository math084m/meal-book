using MealBook.Domain.Recipes;

namespace MealBook.Application.Recipes;

public enum IngredientDeleteResult
{
    Deleted,
    NotFound,
    InUse
}

public interface ICatalogRepository
{
    Task<IReadOnlyList<Term>> GetTermsAsync(CancellationToken cancellationToken);

    Task<IReadOnlyList<Ingredient>> GetIngredientsAsync(CancellationToken cancellationToken);

    Task<IReadOnlyList<ShopSection>> GetShopSectionsAsync(CancellationToken cancellationToken);

    Task<bool> IngredientNameExistsAsync(string nameEn, Guid? exceptId, CancellationToken cancellationToken);

    Task<Ingredient> AddIngredientAsync(Ingredient ingredient, CancellationToken cancellationToken);

    Task<Ingredient?> UpdateIngredientAsync(
        Guid id,
        string nameEn,
        string nameDa,
        string shopSectionSlug,
        CancellationToken cancellationToken);

    Task<IngredientDeleteResult> DeleteIngredientAsync(Guid id, CancellationToken cancellationToken);
}

public sealed class RecipeValidationException(string message) : Exception(message);
