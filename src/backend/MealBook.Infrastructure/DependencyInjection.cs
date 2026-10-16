using MealBook.Application.Recipes;
using MealBook.Infrastructure.Data;
using MealBook.Infrastructure.Translation;
using Microsoft.AspNetCore.DataProtection;
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
        services.AddDataProtection()
            .SetApplicationName("MealBook")
            .PersistKeysToDbContext<RecipeDbContext>();
        services.AddHttpClient<ITranslator, DeepLTranslator>(client =>
        {
            client.BaseAddress = DeepLTranslator.GetBaseAddress(configuration["DeepL:ApiKey"]);
            client.Timeout = TimeSpan.FromSeconds(30);
        });
        services.AddScoped<IRecipeRepository, RecipeRepository>();
        services.AddSingleton<IImageStorage>(_ =>
        {
            var s3 = new MealBook.Infrastructure.Storage.S3ImageStorage(configuration);
            var localPath = configuration["ImageStorage:LocalPath"];
            if (s3.IsConfigured || string.IsNullOrWhiteSpace(localPath))
            {
                return s3;
            }

            s3.Dispose();
            return new MealBook.Infrastructure.Storage.FileSystemImageStorage(localPath);
        });
        services.AddScoped<ICatalogRepository, CatalogRepository>();
        services.AddScoped<MealBook.Application.Planning.IMealPlanRepository, MealPlanRepository>();
        services.AddScoped<MealBook.Application.Favorites.IFavoriteRepository, FavoriteRepository>();
        services.AddScoped<MealBook.Application.Planning.IMealPlanNotifier, PgMealPlanNotifier>();
        services.AddHostedService(provider => new MealPlanChangeListener(
            connectionString,
            provider.GetRequiredService<MealBook.Application.Planning.IPlanChangeBroadcaster>(),
            provider.GetRequiredService<Microsoft.Extensions.Logging.ILogger<MealPlanChangeListener>>()));
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
                    SslMode = ResolveSslMode(uri)
                }.ConnectionString;
            }
        }

        throw new InvalidOperationException(
            "Configure ConnectionStrings:MealBook or a valid PostgreSQL DATABASE_URL.");
    }
    private static SslMode ResolveSslMode(Uri uri)
    {
        var query = uri.Query.TrimStart('?')
            .Split('&', StringSplitOptions.RemoveEmptyEntries)
            .Select(part => part.Split('=', 2))
            .Where(part => part.Length == 2)
            .ToDictionary(part => part[0], part => part[1], StringComparer.OrdinalIgnoreCase);
        if (query.TryGetValue("sslmode", out var value)
            && Enum.TryParse<SslMode>(value.Replace("-", string.Empty), true, out var parsed))
        {
            return parsed;
        }

        // Fly private networking is already encrypted and its Postgres does not offer TLS.
        return uri.Host.EndsWith(".flycast", StringComparison.OrdinalIgnoreCase)
            || uri.Host.EndsWith(".internal", StringComparison.OrdinalIgnoreCase)
            ? SslMode.Disable
            : SslMode.Require;
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
