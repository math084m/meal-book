using System.ComponentModel.DataAnnotations;

namespace MealBook.Application.Pantry;

public sealed record PantryItemResponse(Guid IngredientId, decimal? HaveFraction);

public sealed record PantryResponse(bool UseForShopping, IReadOnlyList<PantryItemResponse> Items);

public sealed class PantrySettingsRequest
{
    public bool UseForShopping { get; init; }
}

public sealed class PantryHaveRequest
{
    // Null restores the automatic pantry default.
    [Range(typeof(decimal), "0", "1")]
    public decimal? HaveFraction { get; init; }
}

public interface IPantryRepository
{
    Task<PantryResponse> GetAsync(string userId, CancellationToken cancellationToken);

    Task SetUseForShoppingAsync(string userId, bool enabled, CancellationToken cancellationToken);

    Task<bool> AddAsync(string userId, Guid ingredientId, CancellationToken cancellationToken);

    Task RemoveAsync(string userId, Guid ingredientId, CancellationToken cancellationToken);

    Task<bool> SetHaveFractionAsync(
        string userId, Guid ingredientId, decimal? fraction, CancellationToken cancellationToken);
}
