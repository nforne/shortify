namespace Shortify.DTOs.UserDTOs
{
    public class UserDto
    {
        public int Id { get; set; }
        public string TenantId { get; set; } = default!;
        public string Email { get; set; } = default!;
        public string DisplayName { get; set; } = default!;
        public string[] Roles { get; set; } = Array.Empty<string>();
        public string Status { get; set; } = "active";
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }
}
