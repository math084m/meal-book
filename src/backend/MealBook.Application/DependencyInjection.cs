using MealBook.Application.Recipes;
using Microsoft.Extensions.DependencyInjection;

namespace MealBook.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddMealBookApplication(this IServiceCollection services)
    {
        services.AddScoped<IRecipeService, RecipeService>();
        return services;
    }
}
