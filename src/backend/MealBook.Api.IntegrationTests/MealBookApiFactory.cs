using MealBook.Application.Recipes;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace MealBook.Api.IntegrationTests;

public sealed class MealBookApiFactory(string connectionString) : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseContentRoot(AppContext.BaseDirectory);
        // UseSetting applies before WebApplication.CreateBuilder reads configuration;
        // ConfigureAppConfiguration is too late for minimal hosting.
        builder.UseSetting("ConnectionStrings:MealBook", connectionString);
        builder.UseSetting("Admin:Email", "admin@mealbook.test");
        builder.UseSetting("Admin:Password", "TestAdmin12345");
        builder.UseSetting("RecipeImport:ApiKey", new string('k', 48));
        builder.ConfigureTestServices(services =>
        {
            services.RemoveAll<ITranslator>();
            services.AddSingleton<ITranslator, FakeTranslator>();
        });
    }
}

public sealed class FakeTranslator : ITranslator
{
    public bool IsConfigured => true;

    public Task<IReadOnlyList<string>> TranslateAsync(
        IReadOnlyList<string> texts,
        string sourceLanguage,
        string targetLanguage,
        CancellationToken cancellationToken) =>
        Task.FromResult<IReadOnlyList<string>>(
            texts.Select(text => $"[{targetLanguage}] {text}").ToArray());
}
