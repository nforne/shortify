using System;

namespace Shortify.DTOs.ResolveDTOs
{
    public class ResolveResponseDto
    {
        public int AttributeId { get; set; }
        public int AccountRootUserPk { get; set; }
        public byte[] Body { get; set; } = Array.Empty<byte>();
        public string ContentType { get; set; } = "application/json";
        public string? Signature { get; set; }
        public bool CacheHit { get; set; }
        public string? RedirectUrl { get; set; } // when set, controller will redirect instead of proxy
    }
}
