using System.Security.Claims;
using MealBook.Application.Accounts;
using MealBook.Infrastructure.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace MealBook.Api.Accounts;

public static class AccountEndpoints
{
    private const string DisplayNameIndex = "IX_AspNetUsers_DisplayName";

    // Separate error keys let the frontend show a localized "taken" message instead of the format rules.
    private const string InvalidDisplayNameKey = "displayName";
    private const string TakenDisplayNameKey = "displayNameTaken";

    public static IEndpointRouteBuilder MapAccountEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapPost("/api/auth/register", RegisterAsync);
        app.MapGet("/api/auth/me", GetCurrentUserAsync).RequireAuthorization();
        app.MapPut("/api/auth/profile", UpdateProfileAsync).RequireAuthorization();
        return app;
    }

    private static async Task<IResult> RegisterAsync(
        RegisterRequest? request,
        UserManager<AppUser> users,
        CancellationToken cancellationToken)
    {
        if (request is null)
        {
            return Results.ValidationProblem(new Dictionary<string, string[]>
            {
                ["request"] = ["A registration request is required."]
            });
        }

        var validationErrors = new Dictionary<string, string[]>();
        var email = request.Email?.Trim() ?? string.Empty;
        var password = request.Password ?? string.Empty;
        if (email.Length > 256 || !new System.ComponentModel.DataAnnotations.EmailAddressAttribute().IsValid(email))
        {
            validationErrors["email"] = ["Enter a valid email address."];
        }

        if (!DisplayNames.TryNormalize(request.DisplayName, out var displayName, out var nameError))
        {
            validationErrors[InvalidDisplayNameKey] = [nameError!];
        }
        else if (await IsDisplayNameTakenAsync(users, displayName, null, cancellationToken))
        {
            validationErrors[TakenDisplayNameKey] = [TakenMessage];
        }

        if (password.Length < 10)
        {
            validationErrors["password"] = ["Password must contain at least 10 characters."];
        }

        if (validationErrors.Count > 0)
        {
            return Results.ValidationProblem(validationErrors);
        }

        var user = new AppUser { UserName = email, Email = email, DisplayName = displayName };
        IdentityResult createResult;
        try
        {
            createResult = await users.CreateAsync(user, password);
        }
        catch (DbUpdateException exception) when (IsDisplayNameConflict(exception))
        {
            return DisplayNameTakenProblem();
        }

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
    }

    private static async Task<IResult> GetCurrentUserAsync(ClaimsPrincipal principal, UserManager<AppUser> users)
    {
        var user = await users.GetUserAsync(principal);
        return user is null ? Results.Unauthorized() : Results.Ok(ToResponse(user, principal));
    }

    private static async Task<IResult> UpdateProfileAsync(
        UpdateProfileRequest? request,
        ClaimsPrincipal principal,
        UserManager<AppUser> users,
        CancellationToken cancellationToken)
    {
        var user = await users.GetUserAsync(principal);
        if (user is null)
        {
            return Results.Unauthorized();
        }

        if (!DisplayNames.TryNormalize(request?.DisplayName, out var displayName, out var nameError))
        {
            return Results.ValidationProblem(
                new Dictionary<string, string[]> { [InvalidDisplayNameKey] = [nameError!] });
        }

        if (await IsDisplayNameTakenAsync(users, displayName, user.Id, cancellationToken))
        {
            return DisplayNameTakenProblem();
        }

        user.DisplayName = displayName;
        try
        {
            var result = await users.UpdateAsync(user);
            if (!result.Succeeded)
            {
                return Results.Problem(
                    title: "Could not update the profile.",
                    detail: string.Join("; ", result.Errors.Select(error => error.Description)),
                    statusCode: StatusCodes.Status500InternalServerError);
            }
        }
        catch (DbUpdateException exception) when (IsDisplayNameConflict(exception))
        {
            return DisplayNameTakenProblem();
        }

        return Results.Ok(ToResponse(user, principal));
    }

    private const string TakenMessage = "That display name is already taken.";

    private static CurrentUserResponse ToResponse(AppUser user, ClaimsPrincipal principal) =>
        new(user.Id, user.Email ?? string.Empty, user.DisplayName, principal.IsInRole("Admin"));

    // Matches the case-insensitive unique index, so "Anna" and "anna" collide.
    private static Task<bool> IsDisplayNameTakenAsync(
        UserManager<AppUser> users,
        string displayName,
        string? exceptUserId,
        CancellationToken cancellationToken)
    {
        var lowered = displayName.ToLowerInvariant();
        return users.Users.AnyAsync(
            user => user.Id != exceptUserId && user.DisplayName != null && user.DisplayName.ToLower() == lowered,
            cancellationToken);
    }

    // The pre-check can race with a concurrent request; the unique index is the final word.
    private static bool IsDisplayNameConflict(DbUpdateException exception) =>
        exception.InnerException is PostgresException
        {
            SqlState: PostgresErrorCodes.UniqueViolation,
            ConstraintName: DisplayNameIndex
        };

    private static IResult DisplayNameTakenProblem() =>
        Results.ValidationProblem(new Dictionary<string, string[]> { [TakenDisplayNameKey] = [TakenMessage] });
}
