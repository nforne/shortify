using System;

namespace Shortify.DTOs.MetricDTOs
{
    // JSON-centric metric generation request
    public class CreateJsonMetricDto
    {
        public string TenantId { get; set; } = null!;
        public string Name { get; set; } = null!;
        public DateTime From { get; set; }
        public DateTime To { get; set; }
        public bool IncludeRaw { get; set; } = true; // default: include raw entity dumps
        public object? Options { get; set; } // future extensibility
    }
}
