using MealBook.Application.Recipes;
using MealBook.Infrastructure.Data;
using Migr8;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;

namespace MealBook.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddMealBookInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var connectionString = GetDatabaseConnectionString(configuration);

        services.AddDbContext<RecipeDbContext>(options => options.UseNpgsql(connectionString));
        services.AddScoped<IRecipeRepository, RecipeRepository>();
        services.AddScoped<IDatabaseHealth, DatabaseHealth>();
        services.AddScoped<AdminUserSeeder>();
        services.AddSingleton(new DatabaseMigrationRunner(connectionString));
        return services;
    }

    private static string GetDatabaseConnectionString(IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("MealBook");
        if (!string.IsNullOrWhiteSpace(connectionString))
        {
            return connectionString;
        }

        var databaseUrl = configuration["DATABASE_URL"];
        if (Uri.TryCreate(databaseUrl, UriKind.Absolute, out var uri)
            && uri.Scheme is "postgres" or "postgresql")
        {
            var credentials = uri.UserInfo.Split(':', 2);
            var database = Uri.UnescapeDataString(uri.AbsolutePath.TrimStart('/'));
            if (credentials.Length == 2 && !string.IsNullOrWhiteSpace(database))
            {
                return new NpgsqlConnectionStringBuilder
                {
                    Host = uri.Host,
                    Port = uri.IsDefaultPort ? 5432 : uri.Port,
                    Username = Uri.UnescapeDataString(credentials[0]),
                    Password = Uri.UnescapeDataString(credentials[1]),
                    Database = database,
                    SslMode = SslMode.Require
                }.ConnectionString;
            }
        }

        throw new InvalidOperationException(
            "Configure ConnectionStrings:MealBook or a valid PostgreSQL DATABASE_URL.");
    }
}

public sealed class DatabaseMigrationRunner(string connectionString)
{
    public void ApplyPendingMigrations()
    {
        var migrationsPath = Path.Combine(AppContext.BaseDirectory, "Migrations");
        Migr8.Database.Migrate(connectionString, Migrations.FromFilesIn(migrationsPath));
    }
}
