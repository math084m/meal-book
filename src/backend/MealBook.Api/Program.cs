using MealBook.Application;
using MealBook.Application.Recipes;
using MealBook.Infrastructure;
using MealBook.Infrastructure.Data;
using Microsoft.AspNetCore.Identity;
using System.Security.Claims;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
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
builder.Services
    .AddMealBookApplication()
    .AddMealBookInfrastructure(builder.Configuration);

var app = builder.Build();

app.Services
    .GetRequiredService<DatabaseMigrationRunner>()
    .ApplyPendingMigrations();

await using (var scope = app.Services.CreateAsyncScope())
{
    await scope.ServiceProvider
        .GetRequiredService<AdminUserSeeder>()
        .SeedAsync();
}

app.UseAuthentication();
app.UseAuthorization();

app.MapGet("/api/health", async (IDatabaseHealth health, CancellationToken cancellationToken) =>
    await health.CanConnectAsync(cancellationToken)
        ? Results.Ok(new { status = "ok" })
        : Results.StatusCode(StatusCodes.Status503ServiceUnavailable));
app.MapGroup("/api/identity").MapIdentityApi<AppUser>();
app.MapPost("/api/auth/register", async (
    RegisterRequest? request,
    UserManager<AppUser> users,
    CancellationToken cancellationToken) =>
{
    var validationErrors = new Dictionary<string, string[]>();
    if (request is null)
    {
        return Results.ValidationProblem(new Dictionary<string, string[]>
        {
            ["request"] = ["A registration request is required."]
        });
    }

    var email = request.Email?.Trim() ?? string.Empty;
    var password = request.Password ?? string.Empty;
    if (email.Length > 256 || !new System.ComponentModel.DataAnnotations.EmailAddressAttribute().IsValid(email))
    {
        validationErrors["email"] = ["Enter a valid email address."];
    }

    if (password.Length < 10)
    {
        validationErrors["password"] = ["Password must contain at least 10 characters."];
    }

    if (validationErrors.Count > 0)
    {
        return Results.ValidationProblem(validationErrors);
    }

    cancellationToken.ThrowIfCancellationRequested();
    var user = new AppUser { UserName = email, Email = email };
    var createResult = await users.CreateAsync(user, password);
    if (!createResult.Succeeded)
    {
        var errors = createResult.Errors
            .GroupBy(error => error.Code)
            .ToDictionary(group => group.Key, group => group.Select(error => error.Description).ToArray());
        return Results.ValidationProblem(errors);
    }

    var roleResult = await users.AddToRoleAsync(user, "User");
    if (!roleResult.Succeeded)
    {
        var cleanup = await users.DeleteAsync(user);
        var details = string.Join("; ", roleResult.Errors.Select(error => error.Description));
        if (!cleanup.Succeeded)
        {
            details += "; account cleanup failed: "
                + string.Join("; ", cleanup.Errors.Select(error => error.Description));
        }

        return Results.Problem(
            title: "Could not assign standard account permissions.",
            detail: details,
            statusCode: StatusCodes.Status500InternalServerError);
    }

    return Results.Ok(new { message = "Account created." });
});
app.MapGet("/api/auth/me", (ClaimsPrincipal principal) =>
        Results.Ok(new CurrentUserResponse(
            principal.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty,
            principal.FindFirstValue(ClaimTypes.Email) ?? string.Empty,
            principal.IsInRole("Admin"))))
    .RequireAuthorization();
app.MapControllers();

app.Run();
