using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Shortify.Data;
using Shortify.DTOs.GroupDTOs;
using Shortify.Models;
using Shortify.Repositories.Interfaces;
using Shortify.Services;

namespace Shortify.Repositories
{
    public partial class GroupRepository : IGroupRepository
    {
        private readonly ShortifyDbContext _db;
        public GroupRepository(ShortifyDbContext db) => _db = db;

        public async Task<IEnumerable<GroupEntity>> GetAllAsync(string tenantId, int page, int pageSize, CancellationToken ct)
        {
            var q = _db.Groups.AsQueryable().Where(g => g.TenantId == tenantId && g.Status != "deleted")
                     .OrderBy(g => g.Id)
                     .Skip((page - 1) * pageSize)
                     .Take(pageSize);
            return await q.ToListAsync(ct);
        }

        public async Task<GroupEntity?> GetByIdAsync(int id, CancellationToken ct)
            => await _db.Groups.SingleOrDefaultAsync(g => g.Id == id && g.Status != "deleted", ct);

        public async Task<GroupEntity?> GetByNameAsync(string tenantId, string name, CancellationToken ct)
            => await _db.Groups.SingleOrDefaultAsync(g => g.TenantId == tenantId && g.Name == name, ct);

        public async Task<GroupEntity> AddAsync(GroupEntity entity, CancellationToken ct)
        {
            _db.Groups.Add(entity);
            await _db.SaveChangesAsync(ct);
            return entity;
        }

        public async Task UpdateAsync(GroupEntity entity, CancellationToken ct)
        {
            _db.Groups.Update(entity);
            await _db.SaveChangesAsync(ct);
        }

        public async Task DeleteAsync(GroupEntity entity, CancellationToken ct)
        {
            _db.Groups.Remove(entity);
            await _db.SaveChangesAsync(ct);
        }






    }
}

namespace Shortify.Repositories
{
    public partial class GroupRepository : IGroupRepository
    {
        private static readonly JsonSerializerOptions _jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = false,
            DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
        };

        // ---------- User-group metadata methods ----------

        public async Task<UserGroupMData> LoadUserMetadataAsync(int groupId, CancellationToken ct = default)
        {
            var group = await _db.Groups.AsNoTracking().SingleOrDefaultAsync(g => g.Id == groupId, ct)
                        ?? throw new NotFoundException("Group not found");

            if (string.IsNullOrWhiteSpace(group.MetadataJson))
                return new UserGroupMData();

            return JsonSerializer.Deserialize<UserGroupMData>(group.MetadataJson, _jsonOptions) ?? new UserGroupMData();
        }

        public async Task SaveUserMetadataAsync(int groupId, UserGroupMData meta, CancellationToken ct = default)
        {
            var group = await _db.Groups.SingleOrDefaultAsync(g => g.Id == groupId, ct)
                        ?? throw new NotFoundException("Group not found");

            group.MetadataJson = JsonSerializer.Serialize(meta, _jsonOptions);
            group.UpdatedAt = DateTime.UtcNow;

            try
            {
                await _db.SaveChangesAsync(ct);
            }
            catch (DbUpdateConcurrencyException)
            {
                throw new ConflictException("Concurrency conflict saving user-group metadata");
            }
        }

        public async Task Ugrp_ReplaceMetadataAsync(int groupId, UserGroupMData newMeta, CancellationToken ct = default)
        {
            var group = await _db.Groups.SingleOrDefaultAsync(g => g.Id == groupId, ct)
                        ?? throw new NotFoundException("Group not found");

            group.MetadataJson = JsonSerializer.Serialize(newMeta, _jsonOptions);
            group.UpdatedAt = DateTime.UtcNow;

            try
            {
                await _db.SaveChangesAsync(ct);
            }
            catch (DbUpdateConcurrencyException)
            {
                throw new ConflictException("Concurrency conflict replacing user-group metadata");
            }
        }

        public async Task<int> Ugrp_CountActiveMembersAsync(int groupId, CancellationToken ct = default)
        {
            var group = await _db.Groups.AsNoTracking().SingleOrDefaultAsync(g => g.Id == groupId, ct)
                        ?? throw new NotFoundException("Group not found");

            if (string.IsNullOrWhiteSpace(group.MetadataJson)) return 0;

            var meta = JsonSerializer.Deserialize<UserGroupMData>(group.MetadataJson, _jsonOptions) ?? new UserGroupMData();
            return meta.Members?.Count(m => string.Equals(m.Status, "active", StringComparison.OrdinalIgnoreCase)) ?? 0;
        }

        public async Task Ugrp_ReplaceMetadataJsonAsync(int groupId, string metadataJson, CancellationToken ct = default)
        {
            var group = await _db.Groups.SingleOrDefaultAsync(g => g.Id == groupId, ct)
                        ?? throw new NotFoundException("Group not found");

            group.MetadataJson = metadataJson ?? string.Empty;
            group.UpdatedAt = DateTime.UtcNow;

            try
            {
                await _db.SaveChangesAsync(ct);
            }
            catch (DbUpdateConcurrencyException)
            {
                throw new ConflictException("Concurrency conflict replacing user-group metadata json");
            }
        }

        // ---------- Attribute-group metadata methods ----------

        public async Task<AttrGroupMData> LoadAttrMetadataAsync(int groupId, CancellationToken ct = default)
        {
            var group = await _db.Groups.AsNoTracking().SingleOrDefaultAsync(g => g.Id == groupId, ct)
                        ?? throw new NotFoundException("Group not found");

            if (string.IsNullOrWhiteSpace(group.MetadataJson))
                return new AttrGroupMData();

            return JsonSerializer.Deserialize<AttrGroupMData>(group.MetadataJson, _jsonOptions) ?? new AttrGroupMData();
        }

        public async Task SaveAttrMetadataAsync(int groupId, AttrGroupMData meta, CancellationToken ct = default)
        {
            var group = await _db.Groups.SingleOrDefaultAsync(g => g.Id == groupId, ct)
                        ?? throw new NotFoundException("Group not found");

            group.MetadataJson = JsonSerializer.Serialize(meta, _jsonOptions);
            group.UpdatedAt = DateTime.UtcNow;

            try
            {
                await _db.SaveChangesAsync(ct);
            }
            catch (DbUpdateConcurrencyException)
            {
                throw new ConflictException("Concurrency conflict saving attribute-group metadata");
            }
        }

        public async Task Agrp_ReplaceMetadataAsync(int groupId, AttrGroupMData newMeta, CancellationToken ct = default)
        {
            var group = await _db.Groups.SingleOrDefaultAsync(g => g.Id == groupId, ct)
                        ?? throw new NotFoundException("Group not found");

            group.MetadataJson = JsonSerializer.Serialize(newMeta, _jsonOptions);
            group.UpdatedAt = DateTime.UtcNow;

            try
            {
                await _db.SaveChangesAsync(ct);
            }
            catch (DbUpdateConcurrencyException)
            {
                throw new ConflictException("Concurrency conflict replacing attribute-group metadata");
            }
        }

        public async Task<int> Agrp_CountActiveAttributesAsync(int groupId, CancellationToken ct = default)
        {
            var group = await _db.Groups.AsNoTracking().SingleOrDefaultAsync(g => g.Id == groupId, ct)
                        ?? throw new NotFoundException("Group not found");

            if (string.IsNullOrWhiteSpace(group.MetadataJson)) return 0;

            var meta = JsonSerializer.Deserialize<AttrGroupMData>(group.MetadataJson, _jsonOptions) ?? new AttrGroupMData();
            return meta.Attributes?.Count(a =>
                       string.Equals(a.Status, "active", StringComparison.OrdinalIgnoreCase)
                       && (a.ExpiryDate == null || a.ExpiryDate > DateTime.UtcNow)) ?? 0;
        }

        public async Task Agrp_ReplaceMetadataJsonAsync(int groupId, string metadataJson, CancellationToken ct = default)
        {
            var group = await _db.Groups.SingleOrDefaultAsync(g => g.Id == groupId, ct)
                        ?? throw new NotFoundException("Group not found");

            group.MetadataJson = metadataJson ?? string.Empty;
            group.UpdatedAt = DateTime.UtcNow;

            try
            {
                await _db.SaveChangesAsync(ct);
            }
            catch (DbUpdateConcurrencyException)
            {
                throw new ConflictException("Concurrency conflict replacing attribute-group metadata json");
            }
        }
    }
}
