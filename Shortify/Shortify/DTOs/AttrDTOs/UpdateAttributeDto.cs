using System;

namespace Shortify.DTOs.AttrDTOs
{
    public class UpdateAttributeDto
    {
        public string? Key { get; set; }
        public string? ValueType { get; set; }
        public string? Visibility { get; set; }
        public string? OptionsJson { get; set; }
        public DateTime? ExpiryDate { get; set; }
    }
}
