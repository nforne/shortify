// File: Auth/IAuthRepository.cs
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Shortify.Models;

namespace Shortify.Repositories.Interfaces
{
    /// <summary>
    /// Persistence contract used by the AuthService.
    /// This is intentionally small and surgical: it reuses existing UserEntity and GroupEntity.
    /// Implementations should use your application's DbContext or an in-memory store for tests.
    /// </summary>
    public interface IAuthRepository
    {
        // -----------------------
        // Tenant / user operations
        // -----------------------

        /// <summary>
        /// Get a user by its numeric Id.
        /// </summary>
        Task<UserEntity?> GetUserByIdAsync(int userId, CancellationToken ct = default);

        /// <summary>
        /// Get a user by tenant id and email (case-insensitive email).
        /// </summary>
        Task<UserEntity?> GetUserByEmailAsync(string tenantId, string email, CancellationToken ct = default);

        Task<UserEntity?> GetUserByEmailAsync(string email, CancellationToken ct = default);

        /// <summary>
        /// Create a new user record (tenant admin or tenant user).
        /// Returns the created entity (with Id populated).
        /// </summary>
        Task<UserEntity> CreateUserAsync(UserEntity user, CancellationToken ct = default);

        /// <summary>
        /// Update an existing user record. Caller is responsible for concurrency handling if required.
        /// Returns the updated entity.
        /// </summary>
        Task<UserEntity> UpdateUserAsync(UserEntity user, CancellationToken ct = default);

        /// <summary>
        /// Find a tenant root user by tenant id (TenantId string) and IsTenant = true.
        /// Returns null if not found.
        /// </summary>
        Task<UserEntity?> GetTenantRootByTenantIdAsync(string tenantId, CancellationToken ct = default);

        /// <summary>
        /// Create a tenant root user (IsTenant = true). This method is separate for clarity but
        /// implementations may simply call CreateUserAsync.
        /// </summary>
        Task<UserEntity> CreateTenantRootAsync(UserEntity tenantRoot, CancellationToken ct = default);

        // -----------------------
        // ApiKey operations
        // -----------------------

        /// <summary>
        /// Get an API key record by its hashed value (stored hash).
        /// Return null if not found or inactive.
        /// </summary>
        Task<ApiKeyEntity?> GetApiKeyByHashAsync(string keyHash, CancellationToken ct = default);

        /// <summary>
        /// Create and persist an ApiKeyEntity (hashed key stored).
        /// </summary>
        Task<ApiKeyEntity> CreateApiKeyAsync(ApiKeyEntity apiKey, CancellationToken ct = default);

        // -----------------------
        // Group metadata helpers
        // -----------------------

        /// <summary>
        /// Get a GroupEntity by id.
        /// </summary>
        Task<GroupEntity?> GetGroupByIdAsync(int groupId, CancellationToken ct = default);

        /// <summary>
        /// Update the GroupEntity (for example after modifying MetadataJson).
        /// Returns the updated entity.
        /// </summary>
        Task<GroupEntity> UpdateGroupAsync(GroupEntity group, CancellationToken ct = default);

        // -----------------------
        // Utility / listing (optional)
        // -----------------------

        /// <summary>
        /// List users for a tenant (paged or full depending on implementation).
        /// Implementations may ignore paging parameters if not needed.
        /// </summary>
        Task<IReadOnlyList<UserEntity>> ListUsersByTenantAsync(string tenantId, CancellationToken ct = default);
    }

    // Minimal ApiKeyEntity definition used by the repository contract.
    // If you already have an ApiKey model in your project, remove this and use that type instead.
    public class ApiKeyEntity
    {
        public int Id { get; set; }
        public string TenantId { get; set; } = default!;
        public string KeyHash { get; set; } = default!; // stored hashed key (e.g., SHA256 hex/base64)
        public string Name { get; set; } = default!;
        public bool IsActive { get; set; } = true;
    }
}
