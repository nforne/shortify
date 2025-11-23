using System.Collections.Generic;

namespace Shortify.DTOs.GroupDTOs
{
    public class PatchGroupDto
    {
        public string? Name { get; set; }
        public string? Type { get; set; }
        public Dictionary<string, string>? Metadata { get; set; }
        public string? Status { get; set; }
    }
}
