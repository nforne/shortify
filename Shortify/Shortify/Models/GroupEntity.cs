using System;

namespace Shortify.Models
{
    public class GroupEntity
    {
        public int Id { get; set; }
        public string TenantId { get; set; } = null!;
        public string Name { get; set; } = null!;
        public string Type { get; set; } = "user";
        public string? MetadataJson { get; set; }
        public string Status { get; set; } = "active";
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public DateTime? DeletedAt { get; set; }
    }
}
