using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Shortify.DTOs.GroupDTOs
{
    public class CreateGroupDto
    {
        [Required]
        public string TenantId { get; set; } = null!;
        [Required]
        public string Name { get; set; } = null!;
        [Required]
        [RegularExpression("^(user|attribute|system)$")]
        public string Type { get; set; } = "user";
        public Dictionary<string, string>? Metadata { get; set; }
    }
}
