using System.ComponentModel.DataAnnotations;

namespace Shortify.DTOs.UserDTOs
{
    public class CreateUserDto
    {
        public required string TenantId { get; set; }
        public required string Email { get; set; }
        public required string FirstName { get; set; }
        public string LastName { get; set; } = string.Empty;
        public string DisplayName { get; set; } = default!;
        public string[] Roles { get; set; } = Array.Empty<string>();

        [Required]
        public required string Password { get;  set; }
    }
}
