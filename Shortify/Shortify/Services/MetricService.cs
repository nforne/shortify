using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Claims;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Shortify.Data;
using Shortify.DTOs.MetricDTOs;
using Shortify.DTOs.UserDTOs;
using Shortify.Models;
using Shortify.RegExtention;
using Shortify.Repositories.Interfaces;
using Shortify.Services.Interfaces;

namespace Shortify.Services
{
    public class MetricService : IMetricService
    {        
        private readonly IMetricRepository _repo;
        private readonly ISnapshotStorage _storage;
        private readonly IAuthContext _auth;
        private readonly ShortifyDbContext _db; // new
        private static readonly JsonSerializerOptions _jsonOptions = new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase, WriteIndented = true };

        public MetricService(IMetricRepository repo, ISnapshotStorage storage, IAuthContext auth, ShortifyDbContext db)
        {
            _repo = repo;
            _storage = storage;
            _auth = auth;
            _db = db;
        }

        public UserDto? GetCurrentUserDto()
        {
            return _auth.User.GetJsonClaim<UserDto>("user");
        }

        // New overload for JSON-first generation
        public async Task<MetricDto> GenerateAsync(CreateJsonMetricDto dto, CancellationToken ct = default)
        {
            //var userId = _auth.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? _auth.User?.FindFirst("sub")?.Value;

            if (dto.From >= dto.To) throw new ArgumentException("From must be before To");
            if (_auth.Role != "admin" && _auth.TenantId != dto.TenantId) throw new ForbiddenException("Tenant mismatch");

            // Upsert or create MetricEntity record
            var existing = await _repo.GetByKeyAsync(dto.TenantId, dto.Name, dto.From, dto.To, ct);
            MetricEntity entity;
            if (existing == null)
            {
                entity = new MetricEntity
                {
                    TenantId = dto.TenantId,
                    Name = dto.Name,
                    From = dto.From,
                    To = dto.To,
                    Status = "pending",
                    FileFormats = new[] { "json", "zip" },
                    CreatedBy = _auth.UserId ?? "system",
                    CreatedAt = DateTime.UtcNow
                };
                entity = await _repo.AddAsync(entity, ct);
            }
            else
            {
                existing.Status = "pending";
                existing.UpdatedAt = DateTime.UtcNow;
                existing.FileFormats = new[] { "json", "zip" };
                entity = existing;
                await _repo.UpdateAsync(entity, ct);
            }

            try
            {
                entity.Status = "running";
                entity.UpdatedAt = DateTime.UtcNow;
                await _repo.UpdateAsync(entity, ct);

                // Prepare temp folder
                var tempDir = Path.Combine(Path.GetTempPath(), "shortify_metrics", Guid.NewGuid().ToString("N"));
                Directory.CreateDirectory(tempDir);

                var files = new List<(string entryName, string filePath)>();

                // Read main entity tables for this tenant and date range.
                // Adjust queries to include columns you want; these are examples.
                // Groups
                var groupsQuery = _db.Groups.AsNoTracking()
                                    .Where(g => g.TenantId == dto.TenantId);
                var groups = await groupsQuery.ToListAsync(ct);
                var groupsJsonPath = Path.Combine(tempDir, "groups.json");
                await WriteObjectToJsonFileAsync(groups, groupsJsonPath, ct);
                files.Add(("groups.json", groupsJsonPath));

                // Attributes
                var attributesQuery = _db.Attributes.AsNoTracking()
                                          .Where(a => a.TenantId == dto.TenantId);
                var attributes = await attributesQuery.ToListAsync(ct);
                var attrsJsonPath = Path.Combine(tempDir, "attributes.json");
                await WriteObjectToJsonFileAsync(attributes, attrsJsonPath, ct);
                files.Add(("attributes.json", attrsJsonPath));

                // Metrics (existing metric records for this tenant)
                var metricsQuery = _db.Metrics.AsNoTracking()
                                      .Where(m => m.TenantId == dto.TenantId && m.CreatedAt >= dto.From && m.CreatedAt <= dto.To);
                var metrics = await metricsQuery.ToListAsync(ct);
                var metricsJsonPath = Path.Combine(tempDir, "metrics_records.json");
                await WriteObjectToJsonFileAsync(metrics, metricsJsonPath, ct);
                files.Add(("metrics_records.json", metricsJsonPath));

                // Optionally include other tables used by metrics (resolve logs, op logs, aggregates)
                // If those DbSets exist, include them similarly.
                // Example placeholder: if _db.ResolveLogs exists:
                // var resolves = await _db.ResolveLogs.AsNoTracking().Where(r => r.TenantId == dto.TenantId && r.Timestamp >= dto.From && r.Timestamp <= dto.To).ToListAsync(ct);
                // var resolvesJsonPath = Path.Combine(tempDir, "resolve_logs.json");
                // await WriteObjectToJsonFileAsync(resolves, resolvesJsonPath, ct);
                // files.Add(("resolve_logs.json", resolvesJsonPath));

                // If IncludeRaw and you want richer raw files, add them here.

                // Create zip
                var zipFileName = $"{dto.Name}_{dto.From:yyyyMMdd}_{dto.To:yyyyMMdd}.zip";
                var zipPath = Path.Combine(tempDir, zipFileName);
                FileArchiveHelper.CreateZipFromFiles(files, zipPath);

                // Deterministic storage key
                var storageKey = $"metrics/{dto.TenantId}/{dto.Name}/{dto.From:yyyyMMdd}_{dto.To:yyyyMMdd}.zip";

                // Upload
                var (key, url, bytes) = await _storage.UploadAsync(zipPath, storageKey, ct);

                // Delete local temp files (including zip) after successful upload
                try
                {
                    // delete zip and all individual files and directory
                    if (File.Exists(zipPath)) File.Delete(zipPath);
                    foreach (var f in files) if (File.Exists(f.filePath)) File.Delete(f.filePath);
                    Directory.Delete(tempDir, true);
                }
                catch
                {
                    // best-effort cleanup; ignore failures
                }

                // Update entity
                entity.Status = "ready";
                entity.StorageKey = key;
                entity.StorageUrl = url;
                entity.FileSizeBytes = bytes;
                entity.SummaryJson = JsonSerializer.Serialize(new { generatedAt = DateTime.UtcNow, files = files.Select(f => f.entryName) }, _jsonOptions);
                entity.UpdatedAt = DateTime.UtcNow;
                await _repo.UpdateAsync(entity, ct);

                return MapToDto(entity);
            }
            catch (Exception ex)
            {
                entity.Status = "failed";
                entity.ErrorMessage = ex.Message;
                entity.UpdatedAt = DateTime.UtcNow;
                await _repo.UpdateAsync(entity, ct);
                throw;
            }
        }

        // Helper: write object to json file with streaming
        private static async Task WriteObjectToJsonFileAsync<T>(T obj, string path, CancellationToken ct)
        {
            var dir = Path.GetDirectoryName(path) ?? ".";
            Directory.CreateDirectory(dir);
            await using var fs = new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.None, 4096, useAsync: true);
            await JsonSerializer.SerializeAsync(fs, obj, obj?.GetType() ?? typeof(object), _jsonOptions, ct);
            await fs.FlushAsync(ct);
        }

        // Keep existing string-based GenerateAsync(CreateMetricDto) if needed - it can call the JSON overload or remain separate.



        public async Task<MetricDto> GenerateAsync(CreateMetricDto dto, CancellationToken ct = default)
        {
            //var userId = _auth.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? _auth.User?.FindFirst("sub")?.Value;

            // validation
            if (dto.From >= dto.To) throw new ArgumentException("From must be before To");
            var formats = dto.FileFormats == null || dto.FileFormats.Length == 0 ? new[] { "csv" } : dto.FileFormats;

            // RBAC & tenant isolation
            if (_auth.Role != "admin" && _auth.TenantId != dto.TenantId) throw new ForbiddenException("Tenant mismatch");

            // upsert-ish: find existing
            var existing = await _repo.GetByKeyAsync(dto.TenantId, dto.Name, dto.From, dto.To, ct);
            MetricEntity entity;
            if (existing == null)
            {
                entity = new MetricEntity
                {
                    TenantId = dto.TenantId,
                    Name = dto.Name,
                    From = dto.From,
                    To = dto.To,
                    Status = "pending",
                    FileFormats = formats,
                    CreatedBy = _auth.UserId ?? "system",
                    CreatedAt = DateTime.UtcNow
                };
                entity = await _repo.AddAsync(entity, ct);
            }
            else
            {
                existing.Status = "pending";
                existing.UpdatedAt = DateTime.UtcNow;
                existing.FileFormats = formats;
                entity = existing;
                await _repo.UpdateAsync(entity, ct);
            }

            try
            {
                entity.Status = "running";
                entity.UpdatedAt = DateTime.UtcNow;
                await _repo.UpdateAsync(entity, ct);

                // orchestration: gather data from providers (demo: generate simple CSVs from seeded data)
                var tempDir = Path.Combine(Path.GetTempPath(), "shortify_metrics", Guid.NewGuid().ToString("N"));
                Directory.CreateDirectory(tempDir);

                var files = new List<(string entryName, string filePath)>();

                // Example provider 1: summary CSV (seeded rows)
                var summaryLines = GenerateSummaryLines(dto.TenantId, dto.Name, dto.From, dto.To);
                var summaryFile = FileArchiveHelper.WriteLinesToTempFile(summaryLines, tempDir, "summary.csv");
                files.Add(("summary.csv", summaryFile));

                // Example provider 2: rawData if requested
                if (dto.IncludeRaw)
                {
                    var rawLines = GenerateRawLines(dto.TenantId, dto.Name, dto.From, dto.To);
                    var rawFile = FileArchiveHelper.WriteLinesToTempFile(rawLines, tempDir, "rawdata.csv");
                    files.Add(("rawdata.csv", rawFile));
                }

                // Additional formats per request: for demo write same CSVs; real implementation would produce Parquet etc.
                // Create zip archive
                var zipPath = Path.Combine(tempDir, $"{dto.Name}_{dto.From:yyyyMMdd}_{dto.To:yyyyMMdd}.zip");
                FileArchiveHelper.CreateZipFromFiles(files, zipPath);

                // deterministic storage key
                var storageKey = $"metrics/{dto.TenantId}/{dto.Name}/{dto.From:yyyyMMdd}_{dto.To:yyyyMMdd}.zip";
                var (key, url, bytes) = await _storage.UploadAsync(zipPath, storageKey, ct);

                // update entity
                entity.Status = "ready";
                entity.StorageKey = key;
                entity.StorageUrl = url;
                entity.FileSizeBytes = bytes;
                entity.SummaryJson = JsonSerializer.Serialize(new { summaryLinesCount = summaryLines.Count(), rawIncluded = dto.IncludeRaw }, _jsonOptions);
                entity.UpdatedAt = DateTime.UtcNow;

                await _repo.UpdateAsync(entity, ct);

                // cleanup temp
                try { Directory.Delete(tempDir, true); } catch { /* best-effort */ }

                return MapToDto(entity);
            }
            catch (Exception ex)
            {
                entity.Status = "failed";
                entity.ErrorMessage = ex.Message;
                entity.UpdatedAt = DateTime.UtcNow;
                await _repo.UpdateAsync(entity, ct);
                throw;
            }
        }

        public async Task<MetricDto?> GetByIdAsync(int id, CancellationToken ct = default)
        {
            var e = await _repo.GetByIdAsync(id, ct);
            return e == null ? null : MapToDto(e);
        }


        public async Task<IEnumerable<MetricDto>> ListAsync(string tenantId, string? name = null, CancellationToken ct = default)
        {
            var list = await _repo.ListAsync(tenantId, name, null, null, ct);
            return list.Select(MapToDto);
        }

        public async Task<IEnumerable<MetricDto>> ListAsync(string? name = null, CancellationToken ct = default)
        {
            var list = await _repo.ListAsync(GetCurrentUserDto().TenantId, name, null, null, ct);
            return list.Select(MapToDto);
        }

        public async Task<MetricDto?> ReplaceAsync(int id, CreateMetricDto dto, CancellationToken ct = default)
        {
            var e = await _repo.GetByIdAsync(id, ct) ?? throw new NotFoundException("Metric not found");
            if (_auth.Role != "admin" && _auth.TenantId != e.TenantId) throw new ForbiddenException("Tenant mismatch");

            // Replace: for demo, call GenerateAsync semantics
            await DeleteStorageIfAny(e, ct);
            await _repo.DeleteAsync(e, ct);
            var created = await GenerateAsync(dto, ct);
            return created;
        }

        public async Task PatchAsync(int id, object patch, CancellationToken ct = default)
        {
            var e = await _repo.GetByIdAsync(id, ct) ?? throw new NotFoundException("Metric not found");
            if (_auth.Role != "admin" && _auth.TenantId != e.TenantId) throw new ForbiddenException("Tenant mismatch");
            // Minimal patch support: nothing implemented here for brevity
            e.UpdatedAt = DateTime.UtcNow;
            await _repo.UpdateAsync(e, ct);
        }



        // inside MetricService
        public async Task PatchAsync(int id, MetricPatchDto patch, CancellationToken ct = default)
        {
            var entity = await _repo.GetByIdAsync(id, ct) ?? throw new NotFoundException("Metric not found");
            if (_auth.Role != "admin" && _auth.TenantId != entity.TenantId) throw new ForbiddenException("Tenant mismatch");

            var changed = false;
            if (!string.IsNullOrEmpty(patch.Name) && patch.Name != entity.Name) { entity.Name = patch.Name; changed = true; }
            if (patch.From.HasValue && patch.To.HasValue)
            {
                if (patch.From.Value >= patch.To.Value) throw new ArgumentException("From must be before To");
                entity.From = patch.From.Value; entity.To = patch.To.Value; changed = true;
            }
            else if (patch.From.HasValue) { entity.From = patch.From.Value; changed = true; }
            else if (patch.To.HasValue) { entity.To = patch.To.Value; changed = true; }

            if (changed)
            {
                entity.UpdatedAt = DateTime.UtcNow;
                await _repo.UpdateAsync(entity, ct);
            }

            if (patch.Regenerate == true)
            {
                // trigger synchronous regeneration using existing CreateJsonMetricDto or CreateMetricDto
                var createDto = new CreateJsonMetricDto
                {
                    TenantId = entity.TenantId,
                    Name = entity.Name,
                    From = entity.From,
                    To = entity.To,
                    IncludeRaw = true
                };
                // call the GenerateAsync overload you implemented
                await GenerateAsync(createDto, ct);
            }
        }

        public async Task<MetricDto?> ReplaceAsync(int id, CreateJsonMetricDto dto, CancellationToken ct = default)
        {
            if (dto.From >= dto.To) throw new ArgumentException("From must be before To");
            // get existing
            var existing = await _repo.GetByIdAsync(id, ct);

            if (existing == null)
            {
                // create-and-generate: set id after AddAsync inside GenerateAsync path or create minimal record then call Generate
                var createdDto = await GenerateAsync(dto, ct);
                // GenerateAsync will add a new MetricEntity and return its DTO
                return createdDto; // caller can interpret as created (201) if it prefers
            }

            // Authorization check
            if (_auth.Role != "admin" && _auth.TenantId != existing.TenantId) throw new Shortify.Core.Exceptions.ForbiddenException("Tenant mismatch");

            // Overwrite metadata on existing entity
            existing.Name = dto.Name;
            existing.From = dto.From;
            existing.To = dto.To;
            existing.FileFormats = new[] { "json", "zip" };
            existing.UpdatedAt = DateTime.UtcNow;
            existing.Status = "pending";
            await _repo.UpdateAsync(existing, ct);

            // Regenerate payload (overwrite deterministic storageKey)
            // Call the JSON-generation flow to rewrite files and update record (this reuses your GenerateAsync(CreateJsonMetricDto))
            var regenerated = await GenerateAsync(dto, ct); // this will upsert by key (tenant,name,from,to). It may create a new entity if keys differ; ensure it updates existing as intended

            // Map and return the updated DTO
            return regenerated;
        }



        public async Task DeleteAsync(int id, CancellationToken ct = default)
        {
            var e = await _repo.GetByIdAsync(id, ct) ?? throw new NotFoundException("Metric not found");
            if (_auth.Role != "admin" && _auth.TenantId != e.TenantId) throw new ForbiddenException("Tenant mismatch");
            await DeleteStorageIfAny(e, ct);
            await _repo.DeleteAsync(e, ct);
        }

        public async Task<byte[]?> DownloadAsync(int id, CancellationToken ct = default)
        {
            var e = await _repo.GetByIdAsync(id, ct) ?? throw new NotFoundException("Metric not found");
            if (_auth.Role != "admin" && _auth.TenantId != e.TenantId) throw new ForbiddenException("Tenant mismatch");
            if (string.IsNullOrEmpty(e.StorageKey)) return null;
            var stream = await _storage.GetReadStreamAsync(e.StorageKey, ct);
            if (stream == null) return null;
            using var ms = new MemoryStream();
            await stream.CopyToAsync(ms, ct);
            return ms.ToArray();
        }

        private async Task DeleteStorageIfAny(MetricEntity e, CancellationToken ct)
        {
            if (!string.IsNullOrEmpty(e.StorageKey))
            {
                try { await _storage.DeleteAsync(e.StorageKey, ct); } catch { /* best-effort */ }
            }
        }

        private static MetricDto MapToDto(MetricEntity e)
            => new MetricDto
            {
                Id = e.Id,
                TenantId = e.TenantId,
                Name = e.Name,
                From = e.From,
                To = e.To,
                Status = e.Status,
                StorageKey = e.StorageKey,
                StorageUrl = e.StorageUrl,
                FileFormats = e.FileFormats,
                FileSizeBytes = e.FileSizeBytes,
                SummaryJson = e.SummaryJson,
                ErrorMessage = e.ErrorMessage,
                CreatedAt = e.CreatedAt,
                UpdatedAt = e.UpdatedAt
            };

        // Demo provider: produce deterministic summary lines
        private static IEnumerable<string> GenerateSummaryLines(string tenantId, string name, DateTime from, DateTime to)
        {
            yield return "metric,tenant,from,to,rows";
            yield return $"{name},{tenantId},{from:O},{to:O},3";
            yield return "campaignA,tenant_abc123,2023-01-01,2023-01-31,123";
            yield return "campaignB,tenant_abc123,2023-01-01,2023-01-31,456";
        }

        private static IEnumerable<string> GenerateRawLines(string tenantId, string name, DateTime from, DateTime to)
        {
            yield return "timestamp,source,type,value";
            yield return $"{DateTime.UtcNow:O},resolve,hit,1";
            yield return $"{DateTime.UtcNow:O},operation,exec,42";
        }

       
    }


}


namespace Shortify.Core.Exceptions
{
    public class NotFoundException : Exception
    {
        public NotFoundException() : base() { }
        public NotFoundException(string message) : base(message) { }
        public NotFoundException(string message, Exception inner) : base(message, inner) { }
    }

    public class ForbiddenException : Exception
    {
        public ForbiddenException() : base() { }
        public ForbiddenException(string message) : base(message) { }
        public ForbiddenException(string message, Exception inner) : base(message, inner) { }
    }
}
