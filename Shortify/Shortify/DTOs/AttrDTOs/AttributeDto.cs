using System;

namespace Shortify.DTOs.AttrDTOs
{
    public class AttributeDto
    {
        public int Id { get; set; }
        public string TenantId { get; set; } = null!;
        public string Key { get; set; } = null!;
        public string ValueType { get; set; } = null!;
        public string Visibility { get; set; } = null!;
        public string? OptionsJson { get; set; }
        public string Status { get; set; } = null!;
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public DateTime? ExpiryDate { get; set; }
    }
}
