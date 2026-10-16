using MealBook.Application.Recipes;

namespace MealBook.Infrastructure.Storage;

// Local development stand-in for the S3 bucket: images live in a directory (a Docker volume in Compose).
public sealed class FileSystemImageStorage(string rootPath) : IImageStorage
{
    private readonly string root = Path.GetFullPath(rootPath);

    public bool IsConfigured => true;

    public async Task PutAsync(string key, Stream content, string contentType, CancellationToken cancellationToken)
    {
        var path = ResolvePath(key);
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        await using (var file = File.Create(path))
        {
            await content.CopyToAsync(file, cancellationToken);
        }

        await File.WriteAllTextAsync(path + ".type", contentType, cancellationToken);
    }

    public Task<StoredImage?> GetAsync(string key, CancellationToken cancellationToken)
    {
        var path = ResolvePath(key);
        if (!File.Exists(path))
        {
            return Task.FromResult<StoredImage?>(null);
        }

        var contentType = File.Exists(path + ".type") ? File.ReadAllText(path + ".type") : "application/octet-stream";
        return Task.FromResult<StoredImage?>(new StoredImage(File.OpenRead(path), contentType));
    }

    public Task DeleteAsync(string key, CancellationToken cancellationToken)
    {
        var path = ResolvePath(key);
        File.Delete(path);
        File.Delete(path + ".type");
        return Task.CompletedTask;
    }

    private string ResolvePath(string key)
    {
        var path = Path.GetFullPath(Path.Combine(root, key));
        if (!path.StartsWith(root + Path.DirectorySeparatorChar, StringComparison.Ordinal))
        {
            throw new ArgumentException("Invalid image key.", nameof(key));
        }

        return path;
    }
}
