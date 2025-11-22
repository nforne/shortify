namespace Shortify.DTOs.UserDTOs
{
    public class CreateUserDto
    {
        public string TenantId { get; set; } = default!;
        public string Email { get; set; } = default!;
        public string DisplayName { get; set; } = default!;
        public string[] Roles { get; set; } = Array.Empty<string>();
    }
}
