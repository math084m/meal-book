using System.Text;
using MealBook.Infrastructure.Storage;
using Xunit;

namespace MealBook.Api.IntegrationTests;

public sealed class FileSystemImageStorageTests : IDisposable
{
    private readonly string root = Path.Combine(Path.GetTempPath(), "mealbook-images-" + Guid.NewGuid().ToString("N"));

    [Fact]
    public async Task StoresReadsAndDeletesImages_WithContentType()
    {
        var storage = new FileSystemImageStorage(root);
        await storage.PutAsync("recipes/abc/image", new MemoryStream([1, 2, 3]), "image/webp", CancellationToken.None);

        var stored = await storage.GetAsync("recipes/abc/image", CancellationToken.None);
        Assert.NotNull(stored);
        Assert.Equal("image/webp", stored.ContentType);
        await using (stored.Content)
        {
            using var copy = new MemoryStream();
            await stored.Content.CopyToAsync(copy);
            Assert.Equal([1, 2, 3], copy.ToArray());
        }

        await storage.DeleteAsync("recipes/abc/image", CancellationToken.None);
        Assert.Null(await storage.GetAsync("recipes/abc/image", CancellationToken.None));
        await storage.DeleteAsync("recipes/abc/image", CancellationToken.None);
    }

    [Theory]
    [InlineData("../outside")]
    [InlineData("recipes/../../outside")]
    [InlineData("")]
    public async Task RejectsKeysOutsideTheRoot(string key)
    {
        var storage = new FileSystemImageStorage(root);
        await Assert.ThrowsAsync<ArgumentException>(() =>
            storage.PutAsync(key, new MemoryStream(Encoding.UTF8.GetBytes("x")), "image/png", CancellationToken.None));
        await Assert.ThrowsAsync<ArgumentException>(() => storage.GetAsync(key, CancellationToken.None));
    }

    public void Dispose()
    {
        if (Directory.Exists(root))
        {
            Directory.Delete(root, recursive: true);
        }
    }
}
