using System.Threading;
using System.Threading.Tasks;

namespace Shortify.Core.Contracts
{
    /// <summary>
    /// Abstraction responsible for providing signing keys for tenants.
    /// Implementations may read from configuration, a secrets store, or KMS.
    /// Returns null when a key is not available for the tenant.
    /// </summary>
    public interface IKeyStore
    {
        /// <summary>
        /// Get the signing key for a tenant. Returns null when no key exists.
        /// </summary>
        Task<string?> GetSigningKeyForTenantAsync(string tenantId, CancellationToken ct = default);
    }
}
