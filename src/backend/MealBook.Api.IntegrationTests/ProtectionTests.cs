using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace MealBook.Api.IntegrationTests;

[Collection(ApiCollection.Name)]
public sealed class ProtectionTests(MealBookApiFixture fixture)
{
    private const string BrowserAgent =
        "Mozilla/5.0 (iPad; CPU OS 17_5 like Mac OS X) AppleWebKit/605.1.15 (KHTML, like Gecko) Version/17.5 Mobile/15E148 Safari/604.1";

    private WebApplicationFactory<Program> WithSettings(params (string Key, string Value)[] settings) =>
        fixture.Factory.WithWebHostBuilder(builder =>
        {
            foreach (var (key, value) in settings)
            {
                builder.UseSetting(key, value);
            }
        });

    private static HttpRequestMessage Get(string path, string? userAgent, string? clientIp = null)
    {
        var request = new HttpRequestMessage(HttpMethod.Get, path);
        if (userAgent is not null)
        {
            request.Headers.TryAddWithoutValidation("User-Agent", userAgent);
        }

        if (clientIp is not null)
        {
            request.Headers.Add("X-Forwarded-For", clientIp);
        }

        return request;
    }

    [Theory]
    [InlineData("python-requests/2.31.0")]
    [InlineData("curl/8.4.0")]
    [InlineData("Scrapy/2.11 (+https://scrapy.org)")]
    [InlineData("Mozilla/5.0 (compatible; GPTBot/1.1; +https://openai.com/gptbot)")]
    [InlineData("Go-http-client/2.0")]
    [InlineData("")]
    public async Task AutomatedClients_AreBlockedFromTheApi(string userAgent)
    {
        await using var factory = WithSettings(("Protection:BlockAutomatedClients", "true"));
        using var client = factory.CreateClient();

        using var blocked = await client.SendAsync(Get("/api/recipes", userAgent));
        Assert.Equal(HttpStatusCode.Forbidden, blocked.StatusCode);
    }

    [Fact]
    public async Task Browsers_HealthChecks_AndTheImportEndpoint_AreNotBlocked()
    {
        await using var factory = WithSettings(("Protection:BlockAutomatedClients", "true"));
        using var client = factory.CreateClient();

        using var browser = await client.SendAsync(Get("/api/recipes", BrowserAgent));
        Assert.Equal(HttpStatusCode.OK, browser.StatusCode);

        using var health = await client.SendAsync(Get("/api/health", "curl/8.4.0"));
        Assert.Equal(HttpStatusCode.OK, health.StatusCode);

        using var import = new HttpRequestMessage(HttpMethod.Post, "/api/recipe-imports")
        {
            Content = JsonContent.Create(new { })
        };
        import.Headers.TryAddWithoutValidation("User-Agent", "python-requests/2.31.0");
        using var imported = await client.SendAsync(import);
        Assert.NotEqual(HttpStatusCode.Forbidden, imported.StatusCode);
        Assert.NotEqual(HttpStatusCode.TooManyRequests, imported.StatusCode);
    }

    [Fact]
    public async Task ApiRequests_AreRateLimitedPerClient_ButHealthChecksAreNot()
    {
        await using var factory = WithSettings(("Protection:Api:PerMinute", "3"));
        using var client = factory.CreateClient();

        for (var index = 0; index < 3; index++)
        {
            using var allowed = await client.SendAsync(Get("/api/recipes", BrowserAgent, "198.51.100.1"));
            Assert.Equal(HttpStatusCode.OK, allowed.StatusCode);
        }

        using var limited = await client.SendAsync(Get("/api/recipes", BrowserAgent, "198.51.100.1"));
        Assert.Equal(HttpStatusCode.TooManyRequests, limited.StatusCode);
        Assert.True(limited.Headers.TryGetValues("Retry-After", out var retryAfter));
        Assert.True(int.Parse(retryAfter.Single()) > 0);

        using var otherVisitor = await client.SendAsync(Get("/api/recipes", BrowserAgent, "203.0.113.9"));
        Assert.Equal(HttpStatusCode.OK, otherVisitor.StatusCode);

        for (var index = 0; index < 10; index++)
        {
            using var health = await client.SendAsync(Get("/api/health", BrowserAgent, "198.51.100.1"));
            Assert.Equal(HttpStatusCode.OK, health.StatusCode);
        }
    }

    [Fact]
    public async Task ForgedForwardedAddresses_DoNotCreateFreshBuckets()
    {
        await using var factory = WithSettings(("Protection:Api:PerMinute", "2"));
        using var client = factory.CreateClient();

        var statuses = new List<HttpStatusCode>();
        for (var index = 0; index < 4; index++)
        {
            using var response = await client.SendAsync(Get("/api/recipes", BrowserAgent, $"not-an-address-{index}"));
            statuses.Add(response.StatusCode);
        }

        Assert.Equal(
            [HttpStatusCode.OK, HttpStatusCode.OK, HttpStatusCode.TooManyRequests, HttpStatusCode.TooManyRequests],
            statuses);
    }

    [Fact]
    public async Task SigningIn_IsRateLimitedMoreStrictly()
    {
        await using var factory = WithSettings(("Protection:Auth:PerMinute", "2"));
        using var client = factory.CreateClient();

        var statuses = new List<HttpStatusCode>();
        for (var index = 0; index < 3; index++)
        {
            using var response = await client.PostAsJsonAsync(
                "/api/identity/login",
                new { email = "nobody@mealbook.test", password = "WrongPassword123" });
            statuses.Add(response.StatusCode);
        }

        Assert.Equal(HttpStatusCode.Unauthorized, statuses[0]);
        Assert.Equal(HttpStatusCode.Unauthorized, statuses[1]);
        Assert.Equal(HttpStatusCode.TooManyRequests, statuses[2]);
    }

    [Fact]
    public async Task Responses_CarryRobotsAndSecurityHeaders()
    {
        using var client = fixture.Factory.CreateClient();
        using var response = await client.GetAsync("/api/health");

        Assert.Contains("noindex", response.Headers.GetValues("X-Robots-Tag").Single());
        Assert.Equal("nosniff", response.Headers.GetValues("X-Content-Type-Options").Single());
        Assert.Equal("DENY", response.Headers.GetValues("X-Frame-Options").Single());
    }
}
