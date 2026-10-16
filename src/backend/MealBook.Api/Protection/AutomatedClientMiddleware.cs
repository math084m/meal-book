using System.Text.RegularExpressions;

namespace MealBook.Api.Protection;

// Turns away the self-identified scraping tools and libraries. This only stops lazy bots, since a
// determined scraper can send any User-Agent, so it complements rate limiting instead of replacing it.
public sealed partial class AutomatedClientMiddleware(RequestDelegate next, ProtectionOptions options)
{
    public Task InvokeAsync(HttpContext context)
    {
        if (!options.BlockAutomatedClients
            || RequestClassifier.Classify(context.Request) == RequestClass.None
            || RequestClassifier.AllowsAutomation(context.Request)
            || !IsAutomated(context.Request.Headers.UserAgent.ToString()))
        {
            return next(context);
        }

        context.Response.StatusCode = StatusCodes.Status403Forbidden;
        return Task.CompletedTask;
    }

    public static bool IsAutomated(string userAgent) =>
        string.IsNullOrWhiteSpace(userAgent) || AutomationPattern().IsMatch(userAgent);

    [GeneratedRegex(
        "bot|crawl|spider|scrap|curl|wget|httpie|python|aiohttp|httpx|requests|urllib|go-http|java/|okhttp|apache|jakarta|libwww|perl|ruby|php|guzzle|node-fetch|axios|undici|got/|superagent|headless|phantom|selenium|puppeteer|playwright|webdriver|postman|insomnia|harvest|archive",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex AutomationPattern();
}
