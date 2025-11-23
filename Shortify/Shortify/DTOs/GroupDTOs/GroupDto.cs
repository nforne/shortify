using System;
using System.Collections.Generic;

namespace Shortify.DTOs.GroupDTOs
{
    public class GroupDto
    {
        public int Id { get; set; }
        public string TenantId { get; set; } = null!;
        public string Name { get; set; } = null!;
        public string Type { get; set; } = null!;
        public Dictionary<string, string> Metadata { get; set; } = new();
        public string Status { get; set; } = "active";
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }
}
