namespace RaceDay.Api.Services;

/// <summary>A downloaded blob: its content stream and MIME type.</summary>
public record BlobDownload(Stream Content, string ContentType);

/// <summary>Thrown when Azure Blob Storage is not configured or cannot be reached.</summary>
public class StorageUnavailableException : Exception
{
    public StorageUnavailableException(string message, Exception? inner = null) : base(message, inner) { }
}

/// <summary>
/// All file handling in RaceDay goes through the API and this abstraction.
/// The MVC web app never talks to Azure directly.
/// </summary>
public interface IBlobStorageService
{
    Task UploadAsync(string blobName, Stream content, string contentType, CancellationToken cancellationToken = default);

    /// <summary>Returns null when the blob does not exist.</summary>
    Task<BlobDownload?> DownloadAsync(string blobName, CancellationToken cancellationToken = default);

    Task DeleteAsync(string blobName, CancellationToken cancellationToken = default);
}
