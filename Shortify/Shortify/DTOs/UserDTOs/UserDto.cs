using System.Text.Json;
using Shortify.Models;

namespace Shortify.DTOs.UserDTOs
{
    public class UserDto
    {
        public int Id { get; set; }
        public string TenantId { get; set; } = default!;
        public string Email { get; set; } = default!;
        public string DisplayName { get; set; } = default!;
        public bool IsTenant { get; set; } = false;
        public string[] Roles { get; set; } = Array.Empty<string>();
        public string Status { get; set; } = "active";
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }

        public static UserDto GetUserDTO(UserEntity user) => new()
        {
            Id = user.Id,
            TenantId = user.TenantId,
            Email = user.Email,
            IsTenant = user.IsTenant,
            DisplayName = user.DisplayName,
            Roles = JsonSerializer.Deserialize<string[]?>(user.RolesJson),
            Status = user.Status,
            CreatedAt = user.CreatedAt,
            UpdatedAt = user.UpdatedAt
        };
    }
}
