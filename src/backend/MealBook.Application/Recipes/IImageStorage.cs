namespace MealBook.Application.Recipes;

public interface IImageStorage
{
    bool IsConfigured { get; }

    Task PutAsync(string key, Stream content, string contentType, CancellationToken cancellationToken);

    Task<StoredImage?> GetAsync(string key, CancellationToken cancellationToken);

    Task DeleteAsync(string key, CancellationToken cancellationToken);
}

public sealed record StoredImage(Stream Content, string ContentType);
