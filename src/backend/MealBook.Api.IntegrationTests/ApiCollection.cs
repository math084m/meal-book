using Xunit;

namespace MealBook.Api.IntegrationTests;

[CollectionDefinition(Name)]
public sealed class ApiCollection : ICollectionFixture<MealBookApiFixture>
{
    public const string Name = "api";
}
