// File: Auth/Ef/EfAuthRepository.cs
using System.Security.Cryptography;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Shortify.Data;        // adjust if your DbContext lives in a different namespace
using Shortify.Models;
using Shortify.Repositories.Interfaces;

namespace Shortify.Repositories
{
    /// <summary>
    /// EF Core implementation of IAuthRepository that reuses the existing application's DbContext and entities.
    /// Adjust the DbContext type and namespaces to match your project layout.
    /// </summary>
    public class AuthRepository : IAuthRepository
    {
        private readonly ShortifyDbContext _db; // replace AppDbContext with your actual DbContext type

        public AuthRepository(ShortifyDbContext db)
        {
            _db = db ?? throw new ArgumentNullException(nameof(db));
        }

        // -----------------------
        // Tenant / user operations
        // -----------------------

        public async Task<UserEntity?> GetUserByIdAsync(int userId, CancellationToken ct = default)
        {
            return await _db.Set<UserEntity>()
                .AsNoTracking()
                .FirstOrDefaultAsync(u => u.Id == userId, ct);
        }

        public async Task<UserEntity?> GetUserByEmailAsync(string tenantId, string email, CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(tenantId) || string.IsNullOrWhiteSpace(email))
                return null;
            return await _db.Set<UserEntity>()
                .FirstOrDefaultAsync(u =>
                    u.TenantId == tenantId &&
                    EF.Functions.Collate(u.Email, "SQL_Latin1_General_CP1_CI_AS") == email, ct);
        }

        public async Task<UserEntity?> GetUserByEmailAsync(string email, CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(email)) return null;
            return await _db.Set<UserEntity>()
                .FirstOrDefaultAsync(u => EF.Functions.Collate(u.Email, "SQL_Latin1_General_CP1_CI_AS") == email, ct);
        }



        public async Task<UserEntity> CreateUserAsync(UserEntity user, CancellationToken ct = default)
        {
            if (user == null) throw new ArgumentNullException(nameof(user));
            _db.Set<UserEntity>().Add(user);
            await _db.SaveChangesAsync(ct);
            return user;
        }

        public async Task<UserEntity> UpdateUserAsync(UserEntity user, CancellationToken ct = default)
        {
            if (user == null) throw new ArgumentNullException(nameof(user));
            _db.Set<UserEntity>().Update(user);
            await _db.SaveChangesAsync(ct);
            return user;
        }

        public async Task<UserEntity?> GetTenantRootByTenantIdAsync(string tenantId, CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(tenantId)) return null;

            return await _db.Set<UserEntity>()
                .AsNoTracking()
                .FirstOrDefaultAsync(u => u.TenantId == tenantId && u.IsTenant, ct);
        }

        public async Task<UserEntity> CreateTenantRootAsync(UserEntity tenantRoot, CancellationToken ct = default)
        {
            if (tenantRoot == null) throw new ArgumentNullException(nameof(tenantRoot));
            _db.Set<UserEntity>().Add(tenantRoot);
            await _db.SaveChangesAsync(ct);
            return tenantRoot;
        }

        // -----------------------
        // ApiKey operations
        // -----------------------

        public async Task<ApiKeyEntity?> GetApiKeyByHashAsync(string keyHash, CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(keyHash)) return null;

            // If you have a dedicated ApiKeys table/entity, replace the below with that entity type.
            // This implementation assumes you added an ApiKeys DbSet mapping to ApiKeyEntity.
            var set = _db.Set<ApiKeyEntity>();
            if (set == null) return null;

            return await set.AsNoTracking()
                .FirstOrDefaultAsync(k => k.KeyHash == keyHash && k.IsActive, ct);
        }

        public async Task<ApiKeyEntity> CreateApiKeyAsync(ApiKeyEntity apiKey, CancellationToken ct = default)
        {
            if (apiKey == null) throw new ArgumentNullException(nameof(apiKey));
            _db.Set<ApiKeyEntity>().Add(apiKey);
            await _db.SaveChangesAsync(ct);
            return apiKey;
        }

        // -----------------------
        // Group metadata helpers
        // -----------------------

        public async Task<GroupEntity?> GetGroupByIdAsync(int groupId, CancellationToken ct = default)
        {
            return await _db.Set<GroupEntity>()
                .FirstOrDefaultAsync(g => g.Id == groupId, ct);
        }

        public async Task<GroupEntity> UpdateGroupAsync(GroupEntity group, CancellationToken ct = default)
        {
            if (group == null) throw new ArgumentNullException(nameof(group));
            _db.Set<GroupEntity>().Update(group);
            await _db.SaveChangesAsync(ct);
            return group;
        }

        // -----------------------
        // Utility / listing
        // -----------------------

        public async Task<IReadOnlyList<UserEntity>> ListUsersByTenantAsync(string tenantId, CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(tenantId)) return Array.Empty<UserEntity>();
            var list = await _db.Set<UserEntity>()
                .Where(u => u.TenantId == tenantId)
                .AsNoTracking()
                .ToListAsync(ct);
            return list;
        }

        // -----------------------
        // Helper: hash raw API key (SHA256 hex)
        // -----------------------
        public static string HashApiKey(string raw)
        {
            if (raw == null) throw new ArgumentNullException(nameof(raw));
            using var sha = SHA256.Create();
            var bytes = sha.ComputeHash(Encoding.UTF8.GetBytes(raw));
            // store as hex string for readability; you can change to Base64 if preferred
            return Convert.ToHexString(bytes).ToLowerInvariant();
        }
    }
}
