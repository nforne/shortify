// File: Repositories/InMemoryAuthRepository.cs
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Shortify.Models;
using Shortify.Repositories.Interfaces;

namespace Shortify.Repositories
{
    /// <summary>
    /// Simple in-memory implementation of IAuthRepository for tests and local development.
    /// Stores the same entity types used by the application (UserEntity, GroupEntity, ApiKeyEntity).
    /// </summary>
    public class InMemoryAuthRepository : IAuthRepository
    {
        private readonly ConcurrentDictionary<int, UserEntity> _users = new();
        private readonly ConcurrentDictionary<int, GroupEntity> _groups = new();
        private readonly ConcurrentDictionary<int, ApiKeyEntity> _keys = new();
        private int _userSeq = 0;
        private int _groupSeq = 0;
        private int _keySeq = 0;

        // -----------------------
        // User / tenant operations
        // -----------------------

        public Task<UserEntity?> GetUserByIdAsync(int userId, CancellationToken ct = default)
        {
            _users.TryGetValue(userId, out var user);
            return Task.FromResult(user);
        }

        /// <summary>
        /// Get a user by tenant id and email (case-insensitive).
        /// </summary>
        public Task<UserEntity?> GetUserByEmailAsync(string tenantId, string email, CancellationToken ct = default)
        {
            if (tenantId == null) throw new ArgumentNullException(nameof(tenantId));
            if (email == null) throw new ArgumentNullException(nameof(email));

            var u = _users.Values.FirstOrDefault(x =>
                string.Equals(x.TenantId, tenantId, StringComparison.OrdinalIgnoreCase)
                && string.Equals(x.Email, email, StringComparison.OrdinalIgnoreCase));

            return Task.FromResult(u);
        }

        /// <summary>
        /// Get a user by email across the entire system (case-insensitive).
        /// </summary>
        public Task<UserEntity?> GetUserByEmailAsync(string email, CancellationToken ct = default)
        {
            if (email == null) throw new ArgumentNullException(nameof(email));

            var u = _users.Values.FirstOrDefault(x =>
                string.Equals(x.Email, email, StringComparison.OrdinalIgnoreCase));

            return Task.FromResult(u);
        }

        public Task<UserEntity> CreateUserAsync(UserEntity user, CancellationToken ct = default)
        {
            if (user == null) throw new ArgumentNullException(nameof(user));

            user.Id = Interlocked.Increment(ref _userSeq);
            // Ensure CreatedAt if not set
            if (user.CreatedAt == default) user.CreatedAt = DateTime.UtcNow;
            _users[user.Id] = CloneUser(user);
            return Task.FromResult(CloneUser(user));
        }

        public Task<UserEntity> UpdateUserAsync(UserEntity user, CancellationToken ct = default)
        {
            if (user == null) throw new ArgumentNullException(nameof(user));
            if (user.Id == 0) throw new ArgumentException("User must have an Id to update", nameof(user));

            user.UpdatedAt = DateTime.UtcNow;
            _users[user.Id] = CloneUser(user);
            return Task.FromResult(CloneUser(user));
        }

        public Task<UserEntity?> GetTenantRootByTenantIdAsync(string tenantId, CancellationToken ct = default)
        {
            if (tenantId == null) throw new ArgumentNullException(nameof(tenantId));

            var u = _users.Values.FirstOrDefault(x =>
                string.Equals(x.TenantId, tenantId, StringComparison.OrdinalIgnoreCase)
                && x.IsTenant);

            return Task.FromResult(u);
        }

        public Task<UserEntity> CreateTenantRootAsync(UserEntity tenantRoot, CancellationToken ct = default)
        {
            if (tenantRoot == null) throw new ArgumentNullException(nameof(tenantRoot));

            tenantRoot.Id = Interlocked.Increment(ref _userSeq);
            if (tenantRoot.CreatedAt == default) tenantRoot.CreatedAt = DateTime.UtcNow;
            _users[tenantRoot.Id] = CloneUser(tenantRoot);
            return Task.FromResult(CloneUser(tenantRoot));
        }

        // -----------------------
        // ApiKey operations
        // -----------------------

        public Task<ApiKeyEntity?> GetApiKeyByHashAsync(string keyHash, CancellationToken ct = default)
        {
            if (keyHash == null) throw new ArgumentNullException(nameof(keyHash));

            var k = _keys.Values.FirstOrDefault(x => x.KeyHash == keyHash && x.IsActive);
            return Task.FromResult(k);
        }

        public Task<ApiKeyEntity> CreateApiKeyAsync(ApiKeyEntity apiKey, CancellationToken ct = default)
        {
            if (apiKey == null) throw new ArgumentNullException(nameof(apiKey));

            apiKey.Id = Interlocked.Increment(ref _keySeq);
            _keys[apiKey.Id] = CloneApiKey(apiKey);
            return Task.FromResult(CloneApiKey(apiKey));
        }

        // -----------------------
        // Group metadata helpers
        // -----------------------

        public Task<GroupEntity?> GetGroupByIdAsync(int groupId, CancellationToken ct = default)
        {
            _groups.TryGetValue(groupId, out var g);
            return Task.FromResult(g);
        }

        public Task<GroupEntity> UpdateGroupAsync(GroupEntity group, CancellationToken ct = default)
        {
            if (group == null) throw new ArgumentNullException(nameof(group));
            if (group.Id == 0) throw new ArgumentException("Group must have an Id to update", nameof(group));

            group.UpdatedAt = DateTime.UtcNow;
            _groups[group.Id] = CloneGroup(group);
            return Task.FromResult(CloneGroup(group));
        }

        // -----------------------
        // Utility / listing
        // -----------------------

        public Task<IReadOnlyList<UserEntity>> ListUsersByTenantAsync(string tenantId, CancellationToken ct = default)
        {
            if (tenantId == null) throw new ArgumentNullException(nameof(tenantId));

            var list = _users.Values
                .Where(x => string.Equals(x.TenantId, tenantId, StringComparison.OrdinalIgnoreCase))
                .Select(CloneUser)
                .ToList()
                .AsReadOnly();

            return Task.FromResult((IReadOnlyList<UserEntity>)list);
        }

        // -----------------------
        // Helpers / cloning
        // -----------------------

        // Simple deep-ish clones to avoid callers mutating in-memory store directly.
        private static UserEntity CloneUser(UserEntity src)
        {
            if (src == null) return null!;
            return new UserEntity
            {
                Id = src.Id,
                TenantId = src.TenantId,
                Email = src.Email,
                IsTenant = src.IsTenant,
                FirstName = src.FirstName,
                LastName = src.LastName,
                // store the backing field value for DisplayName if present
                DisplayName = src.DisplayName,
                RolesJson = src.RolesJson,
                Status = src.Status,
                CreatedAt = src.CreatedAt,
                UpdatedAt = src.UpdatedAt,
                DeletedAt = src.DeletedAt,
                PasswordHash = src.PasswordHash,
                PasswordSalt = src.PasswordSalt,
                PasswordChangedAt = src.PasswordChangedAt
            };
        }

        private static ApiKeyEntity CloneApiKey(ApiKeyEntity src)
        {
            if (src == null) return null!;
            return new ApiKeyEntity
            {
                Id = src.Id,
                TenantId = src.TenantId,
                KeyHash = src.KeyHash,
                Name = src.Name,
                IsActive = src.IsActive
            };
        }

        private static GroupEntity CloneGroup(GroupEntity src)
        {
            if (src == null) return null!;
            return new GroupEntity
            {
                Id = src.Id,
                TenantId = src.TenantId,
                Name = src.Name,
                MetadataJson = src.MetadataJson,
                CreatedAt = src.CreatedAt,
                UpdatedAt = src.UpdatedAt,
                DeletedAt = src.DeletedAt
            };
        }

        // -----------------------
        // Optional helpers used by tests
        // -----------------------

        /// <summary>
        /// Helper to seed a user into the in-memory store (useful for tests).
        /// </summary>
        public UserEntity SeedUser(UserEntity user)
        {
            user.Id = Interlocked.Increment(ref _userSeq);
            if (user.CreatedAt == default) user.CreatedAt = DateTime.UtcNow;
            _users[user.Id] = CloneUser(user);
            return CloneUser(user);
        }

        /// <summary>
        /// Helper to seed a group into the in-memory store (useful for tests).
        /// </summary>
        public GroupEntity SeedGroup(GroupEntity group)
        {
            group.Id = Interlocked.Increment(ref _groupSeq);
            if (group.CreatedAt == default) group.CreatedAt = DateTime.UtcNow;
            _groups[group.Id] = CloneGroup(group);
            return CloneGroup(group);
        }

        /// <summary>
        /// Helper to seed an API key into the in-memory store (useful for tests).
        /// </summary>
        public ApiKeyEntity SeedApiKey(ApiKeyEntity key)
        {
            key.Id = Interlocked.Increment(ref _keySeq);
            _keys[key.Id] = CloneApiKey(key);
            return CloneApiKey(key);
        }

        // Helper: hash raw key with SHA256 (kept for parity with previous implementation)
        public static string HashKey(string raw)
        {
            using var sha = SHA256.Create();
            var bytes = sha.ComputeHash(Encoding.UTF8.GetBytes(raw));
            return Convert.ToHexString(bytes);
        }
    }
}
