using MealBook.Application.Recipes;

namespace MealBook.Infrastructure.Data;

public sealed class DatabaseHealth(RecipeDbContext db) : IDatabaseHealth
{
    public Task<bool> CanConnectAsync(CancellationToken cancellationToken) =>
        db.Database.CanConnectAsync(cancellationToken);
}
