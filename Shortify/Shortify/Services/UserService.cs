using System.Text.Json;
using Shortify.DTOs.UserDTOs;
using Shortify.Models;
using Shortify.Repositories.Interfaces;
using Shortify.Services.Interfaces;

namespace Shortify.Services
{
    public class UserService : IUserService
    {
        private readonly IUserRepository _repo;
        private readonly IAuthContext _auth;

        public UserService(IUserRepository repo, IAuthContext auth)
        {
            _repo = repo;
            _auth = auth;
        }

        private bool IsAdmin() => string.Equals(_auth.Role, "admin", StringComparison.OrdinalIgnoreCase);
        private bool IsAccountRoot() => string.Equals(_auth.Role, "account-root", StringComparison.OrdinalIgnoreCase);

        public async Task<IEnumerable<UserDto>> GetAllAsync(string tenantId, int page = 1, int pageSize = 50)
        {
            if (!IsAdmin())
            {
                // non-admins must provide tenant (or use their tenant header)
                tenantId ??= _auth.TenantId;
            }

            var ents = await _repo.GetAllAsync(tenantId ?? throw new ArgumentException("tenantId required"), page, pageSize);
            return ents.Select(e => ToDto(e));
        }

        public async Task<UserDto?> GetByIdAsync(int id)
        {
            var e = await _repo.GetByIdAsync(id);
            if (e == null) return null;
            if (!IsAdmin() && !string.Equals(e.TenantId, _auth.TenantId, StringComparison.OrdinalIgnoreCase))
                throw new UnauthorizedAccessException("tenant mismatch");
            return ToDto(e);
        }

        public async Task<UserDto> CreateAsync(CreateUserDto dto)
        {
            // tenant enforcement: non-admin cannot create across tenants
            if (!IsAdmin() && !string.Equals(dto.TenantId, _auth.TenantId, StringComparison.OrdinalIgnoreCase))
                throw new UnauthorizedAccessException("cannot create user for other tenant");

            var existing = await _repo.GetByEmailAsync(dto.TenantId, dto.Email);
            if (existing != null) throw new InvalidOperationException("email already exists");

            var ent = new UserEntity
            {
                TenantId = dto.TenantId,
                Email = dto.Email,
                DisplayName = dto.DisplayName,
                RolesJson = JsonSerializer.Serialize(dto.Roles),
                Status = "active",
                CreatedAt = DateTime.UtcNow
            };

            var created = await _repo.AddAsync(ent);
            return ToDto(created);
        }

        public async Task<UserDto?> ReplaceAsync(int id, UserDto dto)
        {
            var e = await _repo.GetByIdAsync(id);
            if (e == null) return null;

            if (!IsAdmin() && !string.Equals(e.TenantId, _auth.TenantId, StringComparison.OrdinalIgnoreCase))
                throw new UnauthorizedAccessException("tenant mismatch");

            // replace fields (tenant change allowed only for admin)
            e.DisplayName = dto.DisplayName;
            e.Email = dto.Email;
            e.RolesJson = JsonSerializer.Serialize(dto.Roles);
            e.Status = dto.Status;
            if (IsAdmin()) e.TenantId = dto.TenantId;

            await _repo.UpdateAsync(e);
            return ToDto(e);
        }

        public async Task<UserDto?> PatchAsync(int id, PatchUserDto dto)
        {
            var e = await _repo.GetByIdAsync(id);
            if (e == null) return null;

            if (!IsAdmin() && !string.Equals(e.TenantId, _auth.TenantId, StringComparison.OrdinalIgnoreCase))
                throw new UnauthorizedAccessException("tenant mismatch");

            if (dto.DisplayName != null) e.DisplayName = dto.DisplayName;
            if (dto.Status != null) e.Status = dto.Status;
            if (dto.Roles != null) e.RolesJson = JsonSerializer.Serialize(dto.Roles);

            await _repo.UpdateAsync(e);
            return ToDto(e);
        }

        public async Task DeleteAsync(int id, bool soft = true)
        {
            var e = await _repo.GetByIdAsync(id);
            if (e == null) return;

            if (!IsAdmin() && !string.Equals(e.TenantId, _auth.TenantId, StringComparison.OrdinalIgnoreCase))
                throw new UnauthorizedAccessException("tenant mismatch");

            if (soft)
            {
                e.Status = "deleted";
                e.DeletedAt = DateTime.UtcNow;
                await _repo.UpdateAsync(e);
            }
            else
            {
                await _repo.DeleteAsync(e);
            }
        }

        private static UserDto ToDto(UserEntity e)
        {
            var roles = Array.Empty<string>();
            try { roles = JsonSerializer.Deserialize<string[]>(e.RolesJson) ?? Array.Empty<string>(); } catch { }
            return new UserDto
            {
                Id = e.Id,
                TenantId = e.TenantId,
                Email = e.Email,
                DisplayName = e.DisplayName,
                Roles = roles,
                Status = e.Status,
                CreatedAt = e.CreatedAt,
                UpdatedAt = e.UpdatedAt
            };
        }
    }
}
