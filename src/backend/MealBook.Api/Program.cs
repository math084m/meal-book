using MealBook.Api.Accounts;
using MealBook.Api.Protection;
using MealBook.Application;
using MealBook.Application.Recipes;
using MealBook.Infrastructure;
using MealBook.Infrastructure.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.FileProviders;
using System.Security.Claims;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddSignalR();
builder.Services.AddSingleton<MealBook.Application.Planning.IPlanChangeBroadcaster, MealBook.Api.Realtime.MealPlanHubBroadcaster>();
builder.Services
    .AddIdentityApiEndpoints<AppUser>(options =>
    {
        options.User.RequireUniqueEmail = true;
        options.Password.RequiredLength = 10;
        options.Password.RequireDigit = true;
        options.Password.RequireUppercase = true;
        options.Password.RequireLowercase = true;
        options.Password.RequireNonAlphanumeric = false;
    })
    .AddRoles<IdentityRole>()
    .AddEntityFrameworkStores<RecipeDbContext>();
builder.Services.AddAuthorization();
// Browsers cannot set an Authorization header on a WebSocket, so the hub accepts the token as a query value.
builder.Services.Configure<Microsoft.AspNetCore.Authentication.BearerToken.BearerTokenOptions>(
    IdentityConstants.BearerScheme,
    options => options.Events.OnMessageReceived = context =>
    {
        if (context.Request.Path.StartsWithSegments(MealBook.Api.Realtime.MealPlanHub.Route)
            && context.Request.Query.TryGetValue("access_token", out var token))
        {
            context.Token = token;
        }

        return Task.CompletedTask;
    });
builder.Services
    .AddMealBookApplication()
    .AddMealBookInfrastructure(builder.Configuration);
builder.AddMealBookProtection();

var app = builder.Build();

app.UseMealBookResponseHeaders();

app.Services
    .GetRequiredService<DatabaseMigrationRunner>()
    .ApplyPendingMigrations();

await using (var scope = app.Services.CreateAsyncScope())
{
    await scope.ServiceProvider
        .GetRequiredService<AdminUserSeeder>()
        .SeedAsync();
}

var serveFrontend = Directory.Exists(Path.Combine(app.Environment.ContentRootPath, "wwwroot"));
PhysicalFileProvider? frontendFileProvider = null;
if (serveFrontend)
{
    frontendFileProvider = new PhysicalFileProvider(
        Path.Combine(app.Environment.ContentRootPath, "wwwroot"));
    app.UseDefaultFiles(new DefaultFilesOptions
    {
        FileProvider = frontendFileProvider
    });
    app.UseStaticFiles(new StaticFileOptions
    {
        FileProvider = frontendFileProvider
    });
}

// Explicit routing after static files; otherwise the catch-all SPA fallback endpoint
// is selected first and static file middleware skips every request.
app.UseMealBookProtection();
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();

app.MapGet("/api/health", async (IDatabaseHealth health, CancellationToken cancellationToken) =>
    await health.CanConnectAsync(cancellationToken)
        ? Results.Ok(new { status = "ok" })
        : Results.StatusCode(StatusCodes.Status503ServiceUnavailable));
app.MapGroup("/api/identity").MapIdentityApi<AppUser>();
app.MapAccountEndpoints();
app.MapControllers();
app.MapHub<MealBook.Api.Realtime.MealPlanHub>(MealBook.Api.Realtime.MealPlanHub.Route);
if (serveFrontend)
{
    app.MapFallbackToFile(
        "{*path:regex(^(?!api(/|$)).*$)}",
        "index.html",
        new StaticFileOptions
        {
            FileProvider = frontendFileProvider
        });
}

app.Run();

public partial class Program;
