// Shortify.Core/Dtos/MetricPatchDto.cs

namespace Shortify.DTOs.MetricDTOs
{
    public class MetricPatchDto
    {
        public string? Name { get; set; }
        public DateTime? From { get; set; }
        public DateTime? To { get; set; }
        public bool? Regenerate { get; set; } // if true, service should regenerate files after applying patch
        public object? Options { get; set; }
    }
}