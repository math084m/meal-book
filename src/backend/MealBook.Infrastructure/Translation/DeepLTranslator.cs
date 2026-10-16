using System.Net.Http.Json;
using System.Text.Json.Serialization;
using MealBook.Application.Recipes;
using Microsoft.Extensions.Configuration;

namespace MealBook.Infrastructure.Translation;

public sealed class DeepLTranslator(HttpClient http, IConfiguration configuration) : ITranslator
{
    private const int MaxTextsPerRequest = 50;

    public bool IsConfigured => !string.IsNullOrWhiteSpace(configuration["DeepL:ApiKey"]);

    public async Task<IReadOnlyList<string>> TranslateAsync(
        IReadOnlyList<string> texts,
        string sourceLanguage,
        string targetLanguage,
        CancellationToken cancellationToken)
    {
        var apiKey = configuration["DeepL:ApiKey"]?.Trim();
        if (string.IsNullOrEmpty(apiKey))
        {
            throw new InvalidOperationException("DeepL is not configured.");
        }

        var results = new List<string>(texts.Count);
        foreach (var batch in texts.Chunk(MaxTextsPerRequest))
        {
            using var request = new HttpRequestMessage(HttpMethod.Post, "v2/translate")
            {
                Content = JsonContent.Create(new DeepLRequest(
                    batch,
                    ToDeepLCode(sourceLanguage, isTarget: false),
                    ToDeepLCode(targetLanguage, isTarget: true)))
            };
            request.Headers.TryAddWithoutValidation("Authorization", $"DeepL-Auth-Key {apiKey}");

            using var response = await http.SendAsync(request, cancellationToken);
            response.EnsureSuccessStatusCode();
            var body = await response.Content.ReadFromJsonAsync<DeepLResponse>(cancellationToken)
                ?? throw new InvalidOperationException("DeepL returned an empty response.");
            if (body.Translations.Count != batch.Length)
            {
                throw new InvalidOperationException("DeepL returned an unexpected number of translations.");
            }

            results.AddRange(body.Translations.Select(item => item.Text));
        }

        return results;
    }

    public static Uri GetBaseAddress(string? apiKey) =>
        new(apiKey?.Trim().EndsWith(":fx", StringComparison.Ordinal) == true
            ? "https://api-free.deepl.com/"
            : "https://api.deepl.com/");

    private static string ToDeepLCode(string language, bool isTarget) =>
        language switch
        {
            "da" => "DA",
            "en" => isTarget ? "EN-GB" : "EN",
            _ => throw new ArgumentOutOfRangeException(nameof(language), language, "Unsupported language.")
        };

    private sealed record DeepLRequest(
        [property: JsonPropertyName("text")] string[] Text,
        [property: JsonPropertyName("source_lang")] string SourceLang,
        [property: JsonPropertyName("target_lang")] string TargetLang);

    private sealed record DeepLResponse(
        [property: JsonPropertyName("translations")] List<DeepLTranslation> Translations);

    private sealed record DeepLTranslation([property: JsonPropertyName("text")] string Text);
}
