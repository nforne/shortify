using System;

namespace Shortify.Models
{
    public class AttributeEntity
    {
        public int Id { get; set; }
        public string TenantId { get; set; } = null!;
        public string Key { get; set; } = null!;
        public string ValueType { get; set; } = "string"; // string | int | bool | date | json
        public string Visibility { get; set; } = "public"; // public | private
        public string? OptionsJson { get; set; } // optional structured options
        public string Status { get; set; } = "active"; // active | deleted
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }
        public DateTime? DeletedAt { get; set; }
        public DateTime? ExpiryDate { get; set; } // optional expiry date

        // new fields for resolve functionality
        public int AccountRootUserPk { get; set; } // immutable once created
        public string? ResolveUrl { get; set; } // the long/public url we will fetch
        public bool ResolveSignedByDefault { get; set; } = false;
        public int? ResolveCacheTtlSeconds { get; set; } // optional per-attribute TTL
        public bool RedirectOnResolve { get; internal set; }
    }
}
