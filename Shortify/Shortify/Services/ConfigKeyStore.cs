using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Shortify.Core.Contracts;

namespace Shortify.Services
{
    /// <summary>
    /// A simple IConfiguration-backed key store suitable for local/dev and small deployments.
    /// Expects configuration shaped like:
    /// "SigningKeys": {
    ///   "tenant_a": "secret-for-tenant-a",
    ///   "tenant_b": "secret-for-tenant-b",
    ///   "default": "global-default-secret"
    /// }
    /// </summary>
    public class ConfigKeyStore : IKeyStore
    {
        private readonly IConfiguration _config;
        private readonly ILogger<ConfigKeyStore> _log;
        private readonly ConcurrentDictionary<string, string?> _cache = new();

        public ConfigKeyStore(IConfiguration config, ILogger<ConfigKeyStore> log)
        {
            _config = config ?? throw new ArgumentNullException(nameof(config));
            _log = log ?? throw new ArgumentNullException(nameof(log));
        }

        public Task<string?> GetSigningKeyForTenantAsync(string tenantId, CancellationToken ct = default)
        {
            if (tenantId == null) throw new ArgumentNullException(nameof(tenantId));
            // Use cached values to avoid repeated IConfiguration lookups under load
            var key = _cache.GetOrAdd(tenantId, id => ResolveFromConfiguration(id));
            return Task.FromResult(key);
        }

        private string? ResolveFromConfiguration(string tenantId)
        {
            try
            {
                // Primary lookup: SigningKeys:{tenantId}
                var section = _config.GetSection("SigningKeys");
                if (section.Exists())
                {
                    var value = section[tenantId];
                    if (!string.IsNullOrEmpty(value)) return value;

                    // fallback to "default" within SigningKeys section
                    var defaultVal = section["default"];
                    if (!string.IsNullOrEmpty(defaultVal))
                    {
                        _log.LogDebug("Using default signing key from SigningKeys:default for tenant {TenantId}", tenantId);
                        return defaultVal;
                    }
                }

                // Secondary lookup: top-level SigningKey_{tenantId} or SigningKey_Default
                var alt = _config[$"SigningKey_{tenantId}"];
                if (!string.IsNullOrEmpty(alt)) return alt;

                var altDefault = _config["SigningKey_Default"];
                if (!string.IsNullOrEmpty(altDefault)) return altDefault;

                _log.LogInformation("No signing key found in configuration for tenant {TenantId}", tenantId);
                return null;
            }
            catch (Exception ex)
            {
                _log.LogWarning(ex, "Failed to resolve signing key for tenant {TenantId}", tenantId);
                return null;
            }
        }
    }
}
