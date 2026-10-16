using System.Security.Cryptography;
using System.Text;
using MealBook.Domain.Recipes;

namespace MealBook.Application.Recipes;

internal sealed record TranslatedIngredient(string Name, string Unit, string? Section, string? Notes);

internal sealed record TranslatedContent(
    string Name,
    string? Description,
    List<string> Tags,
    List<TranslatedIngredient> Ingredients,
    List<string> Steps,
    List<string> Equipment);

// Flattens every translatable text of a recipe in a fixed order so a batch of
// translated strings can be mapped back onto the same structure.
internal static class RecipeTexts
{
    private const int MinUnitLengthToTranslate = 3;

    public static List<string?> Extract(Recipe recipe)
    {
        var texts = new List<string?> { recipe.Name, recipe.Description };
        texts.AddRange(recipe.Tags.OrderBy(item => item.Position).Select(item => item.Name));
        foreach (var ingredient in recipe.Ingredients.OrderBy(item => item.Position))
        {
            texts.Add(ingredient.Name);
            texts.Add(ingredient.Unit.Length >= MinUnitLengthToTranslate ? ingredient.Unit : null);
            texts.Add(ingredient.Section);
            texts.Add(ingredient.Notes);
        }

        texts.AddRange(recipe.Steps.OrderBy(item => item.Position).Select(item => item.Instructions));
        texts.AddRange(recipe.Equipment.OrderBy(item => item.Position).Select(item => item.Name));
        return texts;
    }

    public static string Hash(Recipe recipe)
    {
        var joined = string.Join('\u0001', Extract(recipe).Select(text => text ?? string.Empty));
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(joined))).ToLowerInvariant();
    }

    public static TranslatedContent Build(Recipe recipe, IReadOnlyList<string?> translated)
    {
        var cursor = 0;
        string? Next() => translated[cursor++];

        var name = Next() ?? recipe.Name;
        var description = Next();
        var tags = recipe.Tags.OrderBy(item => item.Position)
            .Select(item => Next() ?? item.Name)
            .ToList();
        var ingredients = recipe.Ingredients.OrderBy(item => item.Position)
            .Select(item => new TranslatedIngredient(
                Next() ?? item.Name,
                Next() ?? item.Unit,
                Next() ?? item.Section,
                Next() ?? item.Notes))
            .ToList();
        var steps = recipe.Steps.OrderBy(item => item.Position)
            .Select(item => Next() ?? item.Instructions)
            .ToList();
        var equipment = recipe.Equipment.OrderBy(item => item.Position)
            .Select(item => Next() ?? item.Name)
            .ToList();
        return new TranslatedContent(name, description, tags, ingredients, steps, equipment);
    }
}
