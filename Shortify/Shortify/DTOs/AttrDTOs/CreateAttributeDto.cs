using System;

namespace Shortify.DTOs.AttrDTOs
{
    public class CreateAttributeDto
    {
        public string TenantId { get; set; } = null!;
        public string Key { get; set; } = null!;
        public string ValueType { get; set; } = "string";
        public string Visibility { get; set; } = "public";
        public string? OptionsJson { get; set; }
        public DateTime? ExpiryDate { get; set; }
    }
}
