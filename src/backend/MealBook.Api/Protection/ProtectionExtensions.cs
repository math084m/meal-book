using System.Globalization;
using System.Threading.RateLimiting;

namespace MealBook.Api.Protection;

public static class ProtectionExtensions
{
    public static ProtectionOptions AddMealBookProtection(this WebApplicationBuilder builder)
    {
        var options = builder.Configuration.GetSection(ProtectionOptions.Section).Get<ProtectionOptions>()
            ?? new ProtectionOptions();
        builder.Services.AddSingleton(options);
        if (!options.RateLimitingEnabled)
        {
            return options;
        }

        builder.Services.AddRateLimiter(limiter =>
        {
            limiter.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
            limiter.OnRejected = (context, _) =>
            {
                var seconds = context.Lease.TryGetMetadata(MetadataName.RetryAfter, out var retryAfter)
                    ? (int)Math.Ceiling(retryAfter.TotalSeconds)
                    : 60;
                context.HttpContext.Response.Headers.RetryAfter =
                    Math.Max(seconds, 1).ToString(CultureInfo.InvariantCulture);
                return ValueTask.CompletedTask;
            };

            // A client has to stay under both the burst limit and the daily ceiling, which stops a
            // patient scraper that keeps just below the per-minute rate.
            limiter.GlobalLimiter = PartitionedRateLimiter.CreateChained(
                Window(options, RequestClass.Api, "api-minute", options.Api.PerMinute, TimeSpan.FromMinutes(1), 6),
                Window(options, RequestClass.Api, "api-day", options.Api.PerDay, TimeSpan.FromDays(1), 24),
                Window(options, RequestClass.Images, "image-minute", options.Images.PerMinute, TimeSpan.FromMinutes(1), 6),
                Window(options, RequestClass.Images, "image-day", options.Images.PerDay, TimeSpan.FromDays(1), 24),
                Window(options, RequestClass.Auth, "auth-minute", options.Auth.PerMinute, TimeSpan.FromMinutes(1), 6),
                Window(options, RequestClass.Auth, "auth-day", options.Auth.PerDay, TimeSpan.FromDays(1), 24));
        });
        return options;
    }

    public static IApplicationBuilder UseMealBookProtection(this WebApplication app)
    {
        var options = app.Services.GetRequiredService<ProtectionOptions>();
        app.UseMiddleware<AutomatedClientMiddleware>();
        if (options.RateLimitingEnabled)
        {
            app.UseRateLimiter();
        }

        return app;
    }

    // Everything is served from this origin (fonts are self-hosted). Angular injects component styles
    // as <style> tags, so styles need 'unsafe-inline'; scripts do not (critical CSS inlining is off).
    // Admin image previews are blob: URLs.
    internal const string ContentSecurityPolicy =
        "default-src 'self'; script-src 'self'; style-src 'self' 'unsafe-inline'; img-src 'self' blob: data:; " +
        "font-src 'self'; connect-src 'self'; manifest-src 'self'; object-src 'none'; base-uri 'self'; " +
        "form-action 'self'; frame-ancestors 'none'";

    // Keeps the site out of search engines and AI training sets, and out of frames and sniffing attacks.
    public static IApplicationBuilder UseMealBookResponseHeaders(this IApplicationBuilder app) =>
        app.Use((context, next) =>
        {
            context.Response.OnStarting(() =>
            {
                var headers = context.Response.Headers;
                headers["X-Robots-Tag"] = "noindex, nofollow, noarchive, noimageindex, noai, noimageai";
                headers["X-Content-Type-Options"] = "nosniff";
                headers["X-Frame-Options"] = "DENY";
                headers["Referrer-Policy"] = "same-origin";
                headers["Permissions-Policy"] = "camera=(), microphone=(), geolocation=(), payment=()";
                headers["Content-Security-Policy"] = ContentSecurityPolicy;
                return Task.CompletedTask;
            });
            return next();
        });

    private static PartitionedRateLimiter<HttpContext> Window(
        ProtectionOptions options,
        RequestClass requestClass,
        string name,
        int permits,
        TimeSpan window,
        int segments) =>
        PartitionedRateLimiter.Create<HttpContext, string>(context =>
            permits > 0 && RequestClassifier.Classify(context.Request) == requestClass
                ? RateLimitPartition.GetSlidingWindowLimiter(
                    $"{name}|{ClientAddress.Resolve(context, options.ClientIpHeader)}",
                    _ => new SlidingWindowRateLimiterOptions
                    {
                        PermitLimit = permits,
                        Window = window,
                        SegmentsPerWindow = segments,
                        QueueLimit = 0,
                        AutoReplenishment = true
                    })
                : RateLimitPartition.GetNoLimiter(name));
}
