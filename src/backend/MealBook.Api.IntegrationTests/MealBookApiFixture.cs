using Xunit;

namespace MealBook.Api.IntegrationTests;

public sealed class MealBookApiFixture : IDisposable
{
    public MealBookApiFixture()
    {
        var connectionString = Environment.GetEnvironmentVariable("MEALBOOK_TEST_CONNECTION_STRING");
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException(
                "Set MEALBOOK_TEST_CONNECTION_STRING to a disposable PostgreSQL test database.");
        }

        Factory = new MealBookApiFactory(connectionString);
    }

    public MealBookApiFactory Factory { get; }

    public void Dispose() => Factory.Dispose();
}
