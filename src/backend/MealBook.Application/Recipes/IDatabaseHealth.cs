namespace MealBook.Application.Recipes;

public interface IDatabaseHealth
{
    Task<bool> CanConnectAsync(CancellationToken cancellationToken);
}
