using System;

namespace Shortify.Models
{
    public class ResolveEventEntity
    {
        public int Id { get; set; }
        public int AttributeId { get; set; }
        public int AccountRootUserPk { get; set; }
        public string? ResolveUrl { get; set; }
        public string? CallerIp { get; set; }
        public string? UserAgent { get; set; }
        public string Outcome { get; set; } = "unknown"; // success | failed | not_found | forbidden | expired
        public bool CacheHit { get; set; } = false;
        public string? ErrorMessage { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
