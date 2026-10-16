using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;

namespace MealBook.Api.IntegrationTests;

public sealed class MealBookApiFactory(string connectionString) : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseContentRoot(AppContext.BaseDirectory);
        builder.ConfigureAppConfiguration((_, configuration) =>
            configuration.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:MealBook"] = connectionString,
                ["Admin:Email"] = "admin@mealbook.test",
                ["Admin:Password"] = "TestAdmin12345",
                ["RecipeImport:ApiKey"] = new string('k', 48)
            }));
    }
}
