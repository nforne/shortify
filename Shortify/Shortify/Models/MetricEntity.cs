using System;

namespace Shortify.Models
{
    public class MetricEntity
    {
        public int Id { get; set; }
        public string TenantId { get; set; } = null!;
        public string Name { get; set; } = null!;
        public DateTime From { get; set; }
        public DateTime To { get; set; }
        public string Status { get; set; } = "pending"; // pending | running | ready | failed
        public string? StorageKey { get; set; }
        public string? StorageUrl { get; set; }
        public string[] FileFormats { get; set; } = new[] { "csv" }; // csv | parquet | json
        public long? FileSizeBytes { get; set; }
        public string? SummaryJson { get; set; }
        public string? ErrorMessage { get; set; }
        public string CreatedBy { get; set; } = null!;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }
        public byte[]? RowVersion { get; set; } // EF concurrency token
    }
}
