using System;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Shortify.Services.Interfaces;

namespace Shortify.Services
{
    public class MetricSnapshotStorage : ISnapshotStorage
    {
        private readonly string _basePath;
        private readonly string _baseUrl; // optional http base to serve files in dev

        public MetricSnapshotStorage(string basePath, string baseUrl = "")
        {
            _basePath = Path.GetFullPath(basePath);
            Directory.CreateDirectory(_basePath);
            _baseUrl = baseUrl?.TrimEnd('/') ?? "";
        }

        public async Task<(string storageKey, string storageUrl, long bytes)> UploadAsync(string localFilePath, string storageKey, CancellationToken ct = default)
        {
            var dest = Path.Combine(_basePath, storageKey.Replace('/', Path.DirectorySeparatorChar));
            var destDir = Path.GetDirectoryName(dest) ?? _basePath;
            Directory.CreateDirectory(destDir);
            // overwrite if exists
            File.Copy(localFilePath, dest, true);
            var fi = new FileInfo(dest);
            var url = string.IsNullOrEmpty(_baseUrl) ? $"file://{dest}" : $"{_baseUrl}/{Uri.EscapeUriString(storageKey)}";
            return (storageKey, url, fi.Length);
        }

        public Task<Stream?> GetReadStreamAsync(string storageKey, CancellationToken ct = default)
        {
            var path = Path.Combine(_basePath, storageKey.Replace('/', Path.DirectorySeparatorChar));
            if (!File.Exists(path)) return Task.FromResult<Stream?>(null);
            Stream s = File.OpenRead(path);
            return Task.FromResult<Stream?>(s);
        }

        public Task DeleteAsync(string storageKey, CancellationToken ct = default)
        {
            var path = Path.Combine(_basePath, storageKey.Replace('/', Path.DirectorySeparatorChar));
            if (File.Exists(path)) File.Delete(path);
            return Task.CompletedTask;
        }

        public Task<string> GetUrlAsync(string storageKey, TimeSpan? expiry = null)
        {
            var url = string.IsNullOrEmpty(_baseUrl) ? $"file://{Path.Combine(_basePath, storageKey.Replace('/', Path.DirectorySeparatorChar))}" : $"{_baseUrl}/{Uri.EscapeUriString(storageKey)}";
            return Task.FromResult(url);
        }
    }

    public static class FileArchiveHelper
    {
        // Write multiple in-memory files or existing temp files into a zip archive.
        // inputs: tuples of (name, localFilePath). The helper will copy local files into zip entries.
        public static void CreateZipFromFiles(IEnumerable<(string entryName, string filePath)> files, string destinationZipPath)
        {
            var destDir = Path.GetDirectoryName(destinationZipPath) ?? ".";
            Directory.CreateDirectory(destDir);
            using var fs = new FileStream(destinationZipPath, FileMode.Create, FileAccess.Write, FileShare.None);
            using var archive = new ZipArchive(fs, ZipArchiveMode.Create, leaveOpen: false);
            foreach (var (entryName, filePath) in files)
            {
                var entry = archive.CreateEntry(entryName, CompressionLevel.Optimal);
                using var entryStream = entry.Open();
                using var src = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read);
                src.CopyTo(entryStream);
            }
        }

        // Helper to create a CSV file from enumerable rows (string[] rows or preformatted lines).
        public static string WriteLinesToTempFile(IEnumerable<string> lines, string tempDir, string fileName)
        {
            Directory.CreateDirectory(tempDir);
            var path = Path.Combine(tempDir, fileName);
            using var sw = new StreamWriter(path, false, Encoding.UTF8);
            foreach (var line in lines)
            {
                sw.WriteLine(line);
            }
            return path;
        }
    }


}
