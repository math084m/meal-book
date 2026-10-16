namespace MealBook.Application.Recipes;

public interface ITranslator
{
    bool IsConfigured { get; }

    Task<IReadOnlyList<string>> TranslateAsync(
        IReadOnlyList<string> texts,
        string sourceLanguage,
        string targetLanguage,
        CancellationToken cancellationToken);
}

public static class RecipeLanguages
{
    public static readonly IReadOnlyList<string> Supported = ["en", "da"];

    public static string? Normalize(string? language)
    {
        var value = language?.Trim().ToLowerInvariant();
        if (string.IsNullOrEmpty(value))
        {
            return null;
        }

        var primary = value.Split('-', '_')[0];
        return Supported.Contains(primary) ? primary : null;
    }

    public static string Other(string language) => language == "da" ? "en" : "da";
}
