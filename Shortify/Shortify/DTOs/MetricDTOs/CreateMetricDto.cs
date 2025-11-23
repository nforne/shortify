using System;

namespace Shortify.DTOs.MetricDTOs
{
    public class CreateMetricDto
    {
        public string TenantId { get; set; } = null!;
        public string Name { get; set; } = null!;
        public DateTime From { get; set; }
        public DateTime To { get; set; }
        public string[]? FileFormats { get; set; } // default to csv if null
        public bool IncludeRaw { get; set; } = false;
        public object? Options { get; set; }
    }
}
