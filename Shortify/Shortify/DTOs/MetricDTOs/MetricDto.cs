using System;

namespace Shortify.DTOs.MetricDTOs
{
    public class MetricDto
    {
        public int Id { get; set; }
        public string TenantId { get; set; } = null!;
        public string Name { get; set; } = null!;
        public DateTime From { get; set; }
        public DateTime To { get; set; }
        public string Status { get; set; } = "pending";
        public string? StorageKey { get; set; }
        public string? StorageUrl { get; set; }
        public string[] FileFormats { get; set; } = new[] { "csv" };
        public long? FileSizeBytes { get; set; }
        public string? SummaryJson { get; set; }
        public string? ErrorMessage { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }
}
