using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;

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
    }
}
