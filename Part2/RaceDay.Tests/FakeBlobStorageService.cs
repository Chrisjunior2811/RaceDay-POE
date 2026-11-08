using System.Collections.Concurrent;
using RaceDay.Api.Services;

namespace RaceDay.Tests;

/// <summary>In-memory stand-in for Azure Blob Storage so the tests need no storage account.</summary>
public class FakeBlobStorageService : IBlobStorageService
{
    private readonly ConcurrentDictionary<string, (byte[] Bytes, string ContentType)> _blobs = new();

    public int Count => _blobs.Count;

    public async Task UploadAsync(string blobName, Stream content, string contentType, CancellationToken cancellationToken = default)
    {
        using var memory = new MemoryStream();
        await content.CopyToAsync(memory, cancellationToken);
        _blobs[blobName] = (memory.ToArray(), contentType);
    }

    public Task<BlobDownload?> DownloadAsync(string blobName, CancellationToken cancellationToken = default)
    {
        BlobDownload? result = null;
        if (_blobs.TryGetValue(blobName, out var blob))
        {
            result = new BlobDownload(new MemoryStream(blob.Bytes), blob.ContentType);
        }

        return Task.FromResult(result);
    }

    public Task DeleteAsync(string blobName, CancellationToken cancellationToken = default)
    {
        _blobs.TryRemove(blobName, out _);
        return Task.CompletedTask;
    }
}
