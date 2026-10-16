using Amazon.Runtime;
using Amazon.S3;
using Amazon.S3.Model;
using MealBook.Application.Recipes;
using Microsoft.Extensions.Configuration;

namespace MealBook.Infrastructure.Storage;

// Stores images in an S3-compatible bucket (Fly Tigris). Stays disabled until the Fly secrets exist.
public sealed class S3ImageStorage : IImageStorage, IDisposable
{
    private readonly IAmazonS3? client;
    private readonly string? bucket;

    public S3ImageStorage(IConfiguration configuration)
    {
        bucket = configuration["BUCKET_NAME"];
        var endpoint = configuration["AWS_ENDPOINT_URL_S3"];
        var accessKey = configuration["AWS_ACCESS_KEY_ID"];
        var secretKey = configuration["AWS_SECRET_ACCESS_KEY"];
        if (string.IsNullOrWhiteSpace(bucket) || string.IsNullOrWhiteSpace(endpoint)
            || string.IsNullOrWhiteSpace(accessKey) || string.IsNullOrWhiteSpace(secretKey))
        {
            return;
        }

        client = new AmazonS3Client(
            new BasicAWSCredentials(accessKey, secretKey),
            new AmazonS3Config
            {
                ServiceURL = endpoint,
                AuthenticationRegion = "auto",
                RequestChecksumCalculation = RequestChecksumCalculation.WHEN_REQUIRED,
                ResponseChecksumValidation = ResponseChecksumValidation.WHEN_REQUIRED
            });
    }

    public bool IsConfigured => client is not null;

    public async Task PutAsync(string key, Stream content, string contentType, CancellationToken cancellationToken)
    {
        await client!.PutObjectAsync(
            new PutObjectRequest
            {
                BucketName = bucket,
                Key = key,
                InputStream = content,
                ContentType = contentType,
                AutoCloseStream = false,
                UseChunkEncoding = false
            },
            cancellationToken);
    }

    public async Task<StoredImage?> GetAsync(string key, CancellationToken cancellationToken)
    {
        try
        {
            var response = await client!.GetObjectAsync(bucket, key, cancellationToken);
            return new StoredImage(response.ResponseStream, response.Headers.ContentType);
        }
        catch (AmazonS3Exception exception) when (exception.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return null;
        }
    }

    public async Task DeleteAsync(string key, CancellationToken cancellationToken)
    {
        await client!.DeleteObjectAsync(bucket, key, cancellationToken);
    }

    public void Dispose() => client?.Dispose();
}
