using System;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using Shortify.Core.Contracts;
using Shortify.Data.Configurations;
using Shortify.DTOs.ResolveDTOs;
using Shortify.Models;
using Shortify.Repositories.Interfaces;
using Shortify.Services.Interfaces;

using System.Net.Http;
using System.Net.Http.Headers;
using Microsoft.AspNetCore.Http;


namespace Shortify.Services
{
    public class ResolveService : IResolveService
    {
        private readonly IAttributeRepository _attrRepo;
        private readonly IResolveEventRepository _eventsRepo;
        private readonly IHttpClientFactory _httpFactory;
        private readonly IDistributedCache _cache;
        private readonly ILogger<ResolveService> _log;
        private readonly IKeyStore _keyStore; // resolves tenant signing keys; optional

        // config
        private readonly TimeSpan _defaultTtl = TimeSpan.FromSeconds(30);

        public ResolveService(
            IAttributeRepository attrRepo,
            IResolveEventRepository eventsRepo,
            IHttpClientFactory httpFactory,
            IDistributedCache cache,
            ILogger<ResolveService> log,
            IKeyStore keyStore // if you don't have a keystore, allow null and skip signing
            )
        {
            _attrRepo = attrRepo;
            _eventsRepo = eventsRepo;
            _httpFactory = httpFactory;
            _cache = cache;
            _log = log;
            _keyStore = keyStore;
        }

        public async Task<ResolveResponseDto> ResolveAsync(int accountRootUserPk, int attrId, HttpRequest request, string returnFmt = "json", CancellationToken ct = default)
        {
            var evt = new ResolveEventEntity
            {
                AccountRootUserPk = accountRootUserPk,
                AttributeId = attrId,
                CallerIp = GetCallerIp(request),
                UserAgent = request.Headers["User-Agent"].ToString(),
                CreatedAt = DateTime.UtcNow
            };

            ResolveResponseDto result = new ResolveResponseDto
            {
                AttributeId = attrId,
                AccountRootUserPk = accountRootUserPk,
                CacheHit = false
            };

            try
            {
                var attr = await _attrRepo.GetByIdAsync(attrId, ct);
                if (attr == null || attr.Status == "deleted")
                {
                    evt.ResolveUrl = null;
                    evt.Outcome = "not_found";
                    await _eventsRepo.AddAsync(evt, ct);
                    throw new Shortify.Core.Exceptions.NotFoundException("Attribute not found");
                }

                // ensure account root user pk matches
                if (attr.AccountRootUserPk != accountRootUserPk)
                {
                    evt.ResolveUrl = attr.ResolveUrl;
                    evt.Outcome = "forbidden";
                    await _eventsRepo.AddAsync(evt, ct);
                    throw new Shortify.Core.Exceptions.ForbiddenException("Account root mismatch");
                }

                // expiry visibility checks
                if (attr.ExpiryDate.HasValue && attr.ExpiryDate.Value <= DateTime.UtcNow)
                {
                    evt.ResolveUrl = attr.ResolveUrl;
                    evt.Outcome = "expired";
                    await _eventsRepo.AddAsync(evt, ct);
                    throw new Shortify.Core.Exceptions.ForbiddenException("Attribute expired");
                }

                if (attr.Visibility == "private")
                {
                    // token/auth check would go here; for now treat as forbidden for unauthenticated
                    // you can add IAuthContext injection if you want to support token checks
                    evt.ResolveUrl = attr.ResolveUrl;
                    evt.Outcome = "forbidden";
                    await _eventsRepo.AddAsync(evt, ct);
                    throw new Shortify.Core.Exceptions.ForbiddenException("Private attribute");
                }

                evt.ResolveUrl = attr.ResolveUrl;

                if (string.IsNullOrEmpty(attr.ResolveUrl))
                {
                    evt.Outcome = "not_found";
                    await _eventsRepo.AddAsync(evt, ct);
                    throw new Shortify.Core.Exceptions.NotFoundException("No resolve url configured");
                }

                // compute cache key
                var key = $"resolve:{accountRootUserPk}:{attr.Id}:{NormalizeUrl(attr.ResolveUrl)}:{returnFmt}";
                var cached = await _cache.GetAsync(key, ct);
                if (cached != null)
                {
                    // cached bytes: we store a small envelope JSON with base64 body, contentType, signature
                    var cachedStr = Encoding.UTF8.GetString(cached);
                    var env = JsonSerializer.Deserialize<CachedEnvelope>(cachedStr);
                    if (env != null)
                    {
                        result.Body = Convert.FromBase64String(env.BodyBase64);
                        result.ContentType = env.ContentType;
                        result.Signature = env.Signature;
                        result.CacheHit = true;

                        evt.CacheHit = true;
                        evt.Outcome = "success";
                        await _eventsRepo.AddAsync(evt, ct);
                        return result;
                    }
                }

                // cache miss: fetch upstream
                var client = _httpFactory.CreateClient("resolve-fetcher");
                using var upstreamResp = await client.GetAsync(attr.ResolveUrl, ct);
                if (!upstreamResp.IsSuccessStatusCode)
                {
                    evt.Outcome = "failed";
                    evt.ErrorMessage = $"Upstream returned {(int)upstreamResp.StatusCode}";
                    await _eventsRepo.AddAsync(evt, ct);
                    throw new Exception($"Upstream fetch failed: {(int)upstreamResp.StatusCode}");
                }

                var bodyBytes = await upstreamResp.Content.ReadAsByteArrayAsync(ct);
                var contentType = upstreamResp.Content.Headers.ContentType?.ToString() ?? "application/octet-stream";

                result.Body = bodyBytes;
                result.ContentType = contentType;

                // signing (optional)
                if (attr.ResolveSignedByDefault && _keyStore != null)
                {
                    try
                    {
                        result.Signature = ComputeHmacSha256Base64(bodyBytes, await _keyStore.GetSigningKeyForTenantAsync(attr.TenantId, ct));
                    }
                    catch (Exception ex)
                    {
                        _log.LogWarning(ex, "Signing failed for attribute {AttrId}", attr.Id);
                    }
                }

                // cache envelope
                var ttl = attr.ResolveCacheTtlSeconds.HasValue ? TimeSpan.FromSeconds(attr.ResolveCacheTtlSeconds.Value) : _defaultTtl;
                var envelope = new CachedEnvelope
                {
                    BodyBase64 = Convert.ToBase64String(result.Body),
                    ContentType = result.ContentType,
                    Signature = result.Signature
                };
                var envBytes = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(envelope));
                var options = new DistributedCacheEntryOptions().SetAbsoluteExpiration(ttl);
                await _cache.SetAsync(key, envBytes, options, ct);

                evt.CacheHit = false;
                evt.Outcome = "success";
                await _eventsRepo.AddAsync(evt, ct);

                return result;
            }
            catch
            {
                // any thrown exception is already logged via early AddAsync calls. Ensure we log final event if missing.
                if (evt.Outcome == "unknown")
                {
                    evt.Outcome = "failed";
                    await _eventsRepo.AddAsync(evt, ct);
                }
                throw;
            }
        }

        private static string GetCallerIp(HttpRequest req)
        {
            if (req.Headers.TryGetValue("X-Forwarded-For", out var v))
            {
                return v.ToString();
            }
            return req.HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        }

        private static string NormalizeUrl(string url) => url.ToLowerInvariant().Trim();

        private static string ComputeHmacSha256Base64(byte[] payload, string key)
        {
            var keyBytes = Encoding.UTF8.GetBytes(key);
            using var hmac = new System.Security.Cryptography.HMACSHA256(keyBytes);
            var sig = hmac.ComputeHash(payload);
            return Convert.ToBase64String(sig);
        }

        private class CachedEnvelope
        {
            public string BodyBase64 { get; set; } = string.Empty;
            public string ContentType { get; set; } = "application/octet-stream";
            public string? Signature { get; set; }
            public string? RedirectUrl { get; set; } // cached redirect target when attr.RedirectOnResolve == true
        }



        private static bool RequestQueryStringAvailable(HttpRequest req) => req?.QueryString.HasValue == true;

        private static void CopyForwardHeaderIfPresent(HttpRequest source, HttpRequestHeaders dest, string headerName)
        {
            if (source.Headers.TryGetValue(headerName, out var values))
            {
                // HttpRequestHeaders.Add accepts IEnumerable<string>
                try { dest.Add(headerName, (IEnumerable<string>)values); } catch { /* ignore invalid adds */ }
            }
        }

    }
}
