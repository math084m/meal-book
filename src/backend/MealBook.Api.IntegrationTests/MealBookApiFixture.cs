using Xunit;

namespace MealBook.Api.IntegrationTests;

public sealed class MealBookApiFixture : IDisposable
{
    private readonly string connectionString = ReadConnectionString();

    public MealBookApiFixture()
    {
        Factory = new MealBookApiFactory(connectionString);
    }

    private static string ReadConnectionString()
    {
        var connectionString = Environment.GetEnvironmentVariable("MEALBOOK_TEST_CONNECTION_STRING");
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException(
                "Set MEALBOOK_TEST_CONNECTION_STRING to a disposable PostgreSQL test database.");
        }

        return connectionString;
    }

    public MealBookApiFactory Factory { get; }

    public MealBookApiFactory CreateSecondInstance() => new(connectionString);

    public void Dispose() => Factory.Dispose();
}
