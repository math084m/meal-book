using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;

namespace MealBook.Infrastructure.Data;

public sealed class AdminUserSeeder(
    UserManager<AppUser> users,
    RoleManager<IdentityRole> roles,
    IConfiguration configuration)
{
    public async Task SeedAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        foreach (var roleName in new[] { "User", "Admin" })
        {
            if (!await roles.RoleExistsAsync(roleName))
            {
                var result = await roles.CreateAsync(new IdentityRole(roleName));
                EnsureSucceeded(result, $"creating role {roleName}");
            }
        }

        var email = configuration["Admin:Email"];
        var password = configuration["Admin:Password"];
        if (string.IsNullOrWhiteSpace(email) && string.IsNullOrWhiteSpace(password))
        {
            return;
        }

        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
        {
            throw new InvalidOperationException(
                "Configure both Admin:Email and Admin:Password to seed the administrator account.");
        }

        var user = await users.FindByEmailAsync(email);
        if (user is null)
        {
            user = new AppUser { UserName = email, Email = email, EmailConfirmed = true };
            var createResult = await users.CreateAsync(user, password);
            EnsureSucceeded(createResult, "creating the configured administrator account");
        }

        if (!await users.IsInRoleAsync(user, "Admin"))
        {
            var roleResult = await users.AddToRoleAsync(user, "Admin");
            EnsureSucceeded(roleResult, "assigning the configured administrator role");
        }
    }

    private static void EnsureSucceeded(IdentityResult result, string operation)
    {
        if (!result.Succeeded)
        {
            var errors = string.Join("; ", result.Errors.Select(error => error.Description));
            throw new InvalidOperationException($"Failed {operation}: {errors}");
        }
    }
}
