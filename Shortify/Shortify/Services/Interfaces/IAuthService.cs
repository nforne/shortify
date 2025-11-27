// File: Auth/IAuthService.cs
using Shortify.Models;
using Shortify.Repositories.Interfaces;

namespace Shortify.Services.Interfaces
{
    /// <summary>
    /// High-level authentication service contract.
    /// Operates on existing UserEntity and uses repository for persistence.
    /// SignUp creates a tenant root user (IsTenant = true) and returns the created user entity.
    /// </summary>
    public interface IAuthService
    {
        /// <summary>
        /// Create a new tenant (tenant root user). The returned UserEntity represents the tenant root (IsTenant = true)
        /// and contains the generated TenantId string.
        /// </summary>
        Task<UserEntity> SignUpTenantAsync(string tenantName, string adminEmail, string password, CancellationToken ct = default);

        Task<UserEntity> SignUpTenantAsync(
              string tenantName,
              string adminEmail,
              string password,
              string? displayName = null,
              string? firstName = null,
              string? lastName = null,
              CancellationToken ct = default);

        /// <summary>
        /// Create a new user under an existing tenant. The created user will have IsTenant = false and TenantId set.
        /// </summary>
        Task<UserEntity> CreateUserForTenantAsync(string tenantId, string email, string password, IEnumerable<string>? roles = null, CancellationToken ct = default);

        Task<UserEntity> CreateUserForTenantAsync(
             string tenantId,
             string email,
             string password,
             IEnumerable<string>? roles = null,
             string? displayName = null,
             string? firstName = null,
             string? lastName = null,
             CancellationToken ct = default);

        /// <summary>
        /// Validate credentials and return a signed JWT (or null if invalid).
        /// </summary>
        Task<string?> SignInAsync(string tenantId, string email, string password, CancellationToken ct = default);

        Task<string?> SignInAsync(string email, string password, CancellationToken ct = default);

        /// <summary>
        /// Create a test token (dev/permissive mode). Throws if not allowed by configuration.
        /// </summary>
        Task<string> CreateTestTokenAsync(string tenantId, string email, IEnumerable<string>? roles = null, CancellationToken ct = default);

        /// <summary>
        /// Validate a raw API key and return the ApiKeyEntity if valid (or null).
        /// </summary>
        Task<ApiKeyEntity?> ValidateApiKeyAsync(string rawKey, CancellationToken ct = default);

        /// <summary>
        /// Get a user by numeric id.
        /// </summary>
        Task<UserEntity?> GetUserByIdAsync(int userId, CancellationToken ct = default);
    }
}
