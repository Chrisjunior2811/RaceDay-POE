using Azure;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;

namespace RaceDay.Api.Services;

/// <summary>
/// Stores event banners and profile pictures in a private Azure Blob Storage container.
/// Works against a real storage account or the Azurite emulator, depending on the connection string.
/// </summary>
public class AzureBlobStorageService : IBlobStorageService
{
    private readonly string? _connectionString;
    private readonly string _containerName;
    private readonly SemaphoreSlim _lock = new(1, 1);
    private BlobContainerClient? _container;

    public AzureBlobStorageService(IConfiguration config)
    {
        _connectionString = config["AzureStorage:ConnectionString"];
        _containerName = config["AzureStorage:ContainerName"] ?? "raceday-images";
    }

    public async Task UploadAsync(string blobName, Stream content, string contentType, CancellationToken cancellationToken = default)
    {
        var container = await GetContainerAsync(cancellationToken);
        try
        {
            var options = new BlobUploadOptions { HttpHeaders = new BlobHttpHeaders { ContentType = contentType } };
            await container.GetBlobClient(blobName).UploadAsync(content, options, cancellationToken);
        }
        catch (Exception ex) when (ex is RequestFailedException or AggregateException)
        {
            throw new StorageUnavailableException("The image could not be saved to storage.", ex);
        }
    }

    public async Task<BlobDownload?> DownloadAsync(string blobName, CancellationToken cancellationToken = default)
    {
        var container = await GetContainerAsync(cancellationToken);
        try
        {
            var response = await container.GetBlobClient(blobName).DownloadStreamingAsync(cancellationToken: cancellationToken);
            return new BlobDownload(response.Value.Content, response.Value.Details.ContentType);
        }
        catch (RequestFailedException ex) when (ex.Status == 404)
        {
            return null;
        }
        catch (Exception ex) when (ex is RequestFailedException or AggregateException)
        {
            throw new StorageUnavailableException("The image could not be read from storage.", ex);
        }
    }

    public async Task DeleteAsync(string blobName, CancellationToken cancellationToken = default)
    {
        var container = await GetContainerAsync(cancellationToken);
        try
        {
            await container.GetBlobClient(blobName).DeleteIfExistsAsync(cancellationToken: cancellationToken);
        }
        catch (Exception ex) when (ex is RequestFailedException or AggregateException)
        {
            throw new StorageUnavailableException("The old image could not be removed from storage.", ex);
        }
    }

    // The container is created (privately) the first time it is needed.
    private async Task<BlobContainerClient> GetContainerAsync(CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(_connectionString))
        {
            throw new StorageUnavailableException("Azure Blob Storage is not configured.");
        }

        if (_container is not null)
        {
            return _container;
        }

        await _lock.WaitAsync(cancellationToken);
        try
        {
            if (_container is null)
            {
                var clientOptions = new BlobClientOptions();
                clientOptions.Retry.MaxRetries = 2;
                clientOptions.Retry.NetworkTimeout = TimeSpan.FromSeconds(10);

                var container = new BlobContainerClient(_connectionString, _containerName, clientOptions);
                await container.CreateIfNotExistsAsync(PublicAccessType.None, cancellationToken: cancellationToken);
                _container = container;
            }

            return _container;
        }
        catch (Exception ex) when (ex is RequestFailedException or AggregateException or FormatException or ArgumentException)
        {
            throw new StorageUnavailableException("Azure Blob Storage could not be reached.", ex);
        }
        finally
        {
            _lock.Release();
        }
    }
}
