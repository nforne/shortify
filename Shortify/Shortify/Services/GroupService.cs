using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Shortify.DTOs.GroupDTOs;
using Shortify.DTOs.UserDTOs;
using Shortify.Models;
using Shortify.RegExtention;
using Shortify.Repositories.Interfaces;
using Shortify.Services.Interfaces;

namespace Shortify.Services
{
    public class GroupService : IGroupService
    {
        private readonly IGroupRepository _repo;
        private readonly IAuthContext _auth;

        public GroupService(IGroupRepository repo, IAuthContext auth)
        {
            _repo = repo;
            _auth = auth;
        }

        public UserDto? GetCurrentUserDto()
        {
            return _auth.User.GetJsonClaim<UserDto>("user");
        }

        public async Task<IEnumerable<GroupDto>> GetAllAsync(string tenantId, int page = 1, int pageSize = 50)
        {
            tenantId = EnforceTenantScope(tenantId);
            var ents = await _repo.GetAllAsync(tenantId, page, pageSize, CancellationToken.None);
            return ents.Select(MapToDto);
        }

        public async Task<IEnumerable<GroupDto>> GetAllAsync(int page = 1, int pageSize = 50)
        {
            var tenantId = EnforceTenantScope(GetCurrentUserDto().TenantId);
            var ents = await _repo.GetAllAsync(tenantId, page, pageSize, CancellationToken.None);
            return ents.Select(MapToDto);
        }

        public async Task<GroupDto?> GetByIdAsync(int id)
        {
            var ent = await _repo.GetByIdAsync(id, CancellationToken.None);
            if (ent == null) return null;
            EnforceAccess(ent.TenantId, "read");
            return MapToDto(ent);
        }

        public async Task<GroupDto> CreateAsync(CreateGroupDto dto)
        {
            EnforceAccess(dto.TenantId, "create");
            var exists = await _repo.GetByNameAsync(dto.TenantId, dto.Name, CancellationToken.None);
            if (exists != null) throw new ConflictException("Group name already exists for tenant");

            var ent = new GroupEntity
            {
                TenantId = dto.TenantId,
                Name = dto.Name,
                Type = dto.Type,
                MetadataJson = dto.Metadata is null ? null : JsonSerializer.Serialize(dto.Metadata),
                Status = "active",
                CreatedAt = DateTime.UtcNow
            };

            await _repo.AddAsync(ent, CancellationToken.None);
            return MapToDto(ent);
        }

        public async Task<GroupDto?> ReplaceAsync(int id, GroupDto dto)
        {
            var ent = await _repo.GetByIdAsync(id, CancellationToken.None);
            if (ent == null) return null;
            EnforceAccess(ent.TenantId, "update");

            if (dto.Name != ent.Name)
            {
                var dup = await _repo.GetByNameAsync(ent.TenantId, dto.Name, CancellationToken.None);
                if (dup != null && dup.Id != id) throw new ConflictException("Group name conflict");
            }

            ent.Name = dto.Name;
            ent.Type = dto.Type;
            ent.MetadataJson = JsonSerializer.Serialize(dto.Metadata ?? new Dictionary<string, string>());
            ent.UpdatedAt = DateTime.UtcNow;

            await _repo.UpdateAsync(ent, CancellationToken.None);
            return MapToDto(ent);
        }

        public async Task<GroupDto?> PatchAsync(int id, PatchGroupDto dto)
        {
            var ent = await _repo.GetByIdAsync(id, CancellationToken.None);
            if (ent == null) return null;
            EnforceAccess(ent.TenantId, "update");

            if (dto.Name != null) ent.Name = dto.Name;
            if (dto.Type != null) ent.Type = dto.Type;
            if (dto.Metadata != null) ent.MetadataJson = JsonSerializer.Serialize(dto.Metadata);
            if (dto.Status != null) ent.Status = dto.Status;

            ent.UpdatedAt = DateTime.UtcNow;
            await _repo.UpdateAsync(ent, CancellationToken.None);
            return MapToDto(ent);
        }

        public async Task DeleteAsync(int id, bool soft = true)
        {
            var ent = await _repo.GetByIdAsync(id, CancellationToken.None);
            if (ent == null) throw new NotFoundException("Group not found");
            EnforceAccess(ent.TenantId, "delete");

            if (soft)
            {
                ent.Status = "deleted";
                ent.DeletedAt = DateTime.UtcNow;
                ent.UpdatedAt = DateTime.UtcNow;
                await _repo.UpdateAsync(ent, CancellationToken.None);
            }
            else
            {
                await _repo.DeleteAsync(ent, CancellationToken.None);
            }
        }

        private void EnforceAccess(string resourceTenantId, string action)
        {
            var role = _auth.Role;
            var callerTenant = _auth.TenantId;

            if (role == "admin") return;

            if (role == "account-root" || role == "user")
            {
                if (callerTenant != resourceTenantId)
                {
                    throw new ForbiddenException("Cross-tenant access denied");
                }

                if (role == "user" && (action == "create" || action == "delete"))
                    throw new ForbiddenException("Insufficient role");
            }
        }

        private string EnforceTenantScope(string? requestedTenant)
        {
            if (_auth.Role == "admin") return requestedTenant ?? _auth.TenantId ?? throw new ForbiddenException("Tenant required");
            return requestedTenant ?? _auth.TenantId ?? throw new ForbiddenException("Tenant required");
        }

        private static GroupDto MapToDto(GroupEntity e)
            => new()
            {
                Id = e.Id,
                TenantId = e.TenantId,
                Name = e.Name,
                Type = e.Type,
                Metadata = string.IsNullOrEmpty(e.MetadataJson) ? new Dictionary<string, string>() : JsonSerializer.Deserialize<Dictionary<string, string>>(e.MetadataJson)!,
                Status = e.Status,
                CreatedAt = e.CreatedAt,
                UpdatedAt = e.UpdatedAt
            };
    }

    // domain exceptions used above; keep minimal
    public class ConflictException : Exception { public ConflictException(string m) : base(m) { } }
    public class NotFoundException : Exception { public NotFoundException(string m) : base(m) { } }
    public class ForbiddenException : Exception { public ForbiddenException(string m) : base(m) { } }
}
