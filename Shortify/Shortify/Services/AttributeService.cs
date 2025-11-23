using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Shortify.DTOs.AttrDTOs;
using Shortify.Models;
using Shortify.Repositories.Interfaces;
using Shortify.Services.Interfaces;

namespace Shortify.Services
{
    public class AttributeService : IAttributeService
    {
        private readonly IAttributeRepository _repo;
        private readonly IAuthContext _auth;

        public AttributeService(IAttributeRepository repo, IAuthContext auth)
        {
            _repo = repo;
            _auth = auth;
        }

        public async Task<AttributeDto> CreateAsync(CreateAttributeDto dto, CancellationToken ct = default)
        {
            if (string.IsNullOrEmpty(dto.TenantId)) throw new ArgumentException("TenantId required");
            if (dto.FromIsInvalid()) { /* skip; not relevant */ }

            if (_auth.Role != "admin" && _auth.TenantId != dto.TenantId) throw new ForbiddenException("Tenant mismatch");

            // uniqueness per tenant:key among non-deleted
            var exists = await _repo.GetByKeyAsync(dto.TenantId, dto.Key, ct);
            if (exists != null) throw new ConflictException("Attribute key already exists");

            var entity = new AttributeEntity
            {
                TenantId = dto.TenantId,
                Key = dto.Key,
                ValueType = dto.ValueType ?? "string",
                Visibility = dto.Visibility ?? "public",
                OptionsJson = dto.OptionsJson,
                ExpiryDate = dto.ExpiryDate,
                Status = "active",
                CreatedAt = DateTime.UtcNow
            };

            var created = await _repo.AddAsync(entity, ct);
            return Map(created);
        }

        public async Task<AttributeDto?> GetByIdAsync(int id, CancellationToken ct = default)
        {
            var e = await _repo.GetByIdAsync(id, ct);
            if (e == null) return null;
            if (_auth.Role != "admin" && _auth.TenantId != e.TenantId && e.Visibility == "private") throw new ForbiddenException("Not allowed");
            return Map(e);
        }

        public async Task<IEnumerable<AttributeDto>> ListAsync(string tenantId, bool includePrivate = false, CancellationToken ct = default)
        {
            if (_auth.Role != "admin" && _auth.TenantId != tenantId && includePrivate) throw new ForbiddenException("Not allowed to see private attributes");
            var list = await _repo.ListAsync(tenantId, includePrivate, ct);
            return list.Select(Map);
        }

        public async Task UpdateAsync(int id, UpdateAttributeDto dto, CancellationToken ct = default)
        {
            var e = await _repo.GetByIdAsync(id, ct) ?? throw new NotFoundException("Attribute not found");
            if (_auth.Role != "admin" && _auth.TenantId != e.TenantId) throw new ForbiddenException("Tenant mismatch");

            if (!string.IsNullOrEmpty(dto.Key) && dto.Key != e.Key)
            {
                var dup = await _repo.GetByKeyAsync(e.TenantId, dto.Key, ct);
                if (dup != null) throw new ConflictException("Key already in use");
                e.Key = dto.Key;
            }

            if (!string.IsNullOrEmpty(dto.ValueType)) e.ValueType = dto.ValueType;
            if (!string.IsNullOrEmpty(dto.Visibility)) e.Visibility = dto.Visibility;
            if (dto.OptionsJson != null) e.OptionsJson = dto.OptionsJson;
            if (dto.ExpiryDate.HasValue) e.ExpiryDate = dto.ExpiryDate;

            e.UpdatedAt = DateTime.UtcNow;
            await _repo.UpdateAsync(e, ct);
        }

        public async Task DeleteAsync(int id, CancellationToken ct = default)
        {
            var e = await _repo.GetByIdAsync(id, ct) ?? throw new NotFoundException("Attribute not found");
            if (_auth.Role != "admin" && _auth.TenantId != e.TenantId) throw new ForbiddenException("Tenant mismatch");
            await _repo.SoftDeleteAsync(e, ct);
        }

        public async Task ToggleVisibilityAsync(int id, string visibility, CancellationToken ct = default)
        {
            if (visibility != "public" && visibility != "private") throw new ArgumentException("Invalid visibility");
            var e = await _repo.GetByIdAsync(id, ct) ?? throw new NotFoundException("Attribute not found");
            if (_auth.Role != "admin" && _auth.TenantId != e.TenantId) throw new ForbiddenException("Tenant mismatch");
            e.Visibility = visibility;
            e.UpdatedAt = DateTime.UtcNow;
            await _repo.UpdateAsync(e, ct);
        }

        private static AttributeDto Map(AttributeEntity e) => new AttributeDto
        {
            Id = e.Id,
            TenantId = e.TenantId,
            Key = e.Key,
            ValueType = e.ValueType,
            Visibility = e.Visibility,
            OptionsJson = e.OptionsJson,
            Status = e.Status,
            CreatedAt = e.CreatedAt,
            UpdatedAt = e.UpdatedAt,
            ExpiryDate = e.ExpiryDate
        };
    }

    // small helpers/exceptions could be resolved from Shortify.Core.Exceptions
    public static class AttributeDtoExtensions
    {
        public static bool FromIsInvalid(this CreateAttributeDto d) => false; // placeholder
    }

    //// inline exceptions - centralize in core exceptions in your project
    //public class ForbiddenException : Exception { public ForbiddenException(string m) : base(m) { } }
    //public class NotFoundException : Exception { public NotFoundException(string m) : base(m) { } }
    //public class ConflictException : Exception { public ConflictException(string m) : base(m) { } }
}
