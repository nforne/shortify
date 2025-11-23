using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Shortify.Services.Interfaces
{
    public interface ISnapshotStorage
    {
        Task<(string storageKey, string storageUrl, long bytes)> UploadAsync(string localFilePath, string storageKey, CancellationToken ct = default);
        Task<Stream?> GetReadStreamAsync(string storageKey, CancellationToken ct = default);
        Task DeleteAsync(string storageKey, CancellationToken ct = default);
        Task<string> GetUrlAsync(string storageKey, TimeSpan? expiry = null);
    }
}
