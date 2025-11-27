//using System;
//using System.Collections.Generic;
//using System.Net.Http;
//using System.Net.Http.Headers;
//using System.Text;
//using System.Text.Json;
//using System.Threading;
//using System.Threading.Tasks;
//using Microsoft.AspNetCore.Http;
//using Microsoft.Extensions.Caching.Distributed;
//using Microsoft.Extensions.Logging;
//using Shortify.Core.Contracts;
//using Shortify.DTOs.ResolveDTOs;
//using Shortify.Models;
//using Shortify.Repositories.Interfaces;
//using Shortify.Services.Interfaces;

//namespace Shortify.Services
//{
//    public class ResolveService : IResolveService
//    {
//        private readonly IAttributeRepository _attrRepo;
//        private readonly IResolveEventRepository _eventsRepo;
//        private readonly IHttpClientFactory _httpFactory;
//        private readonly IDistributedCache _cache;
//        private readonly ILogger<ResolveService> _log;
//        private readonly IKeyStore? _keyStore; // optional

//        private readonly TimeSpan _defaultTtl = TimeSpan.FromSeconds(30);

//        public ResolveService(
//            IAttributeRepository attrRepo,
//            IResolveEventRepository eventsRepo,
//            IHttpClientFactory httpFactory,
//            IDistributedCache cache,
//            ILogger<ResolveService> log,
//            IKeyStore? keyStore = null)
//        {
//            _attrRepo = attrRepo ?? throw new ArgumentNullException(nameof(attrRepo));
//            _eventsRepo = eventsRepo ?? throw new ArgumentNullException(nameof(eventsRepo));
//            _httpFactory = httpFactory ?? throw new ArgumentNullException(nameof(httpFactory));
//            _cache = cache ?? throw new ArgumentNullException(nameof(cache));
//            _log = log ?? throw new ArgumentNullException(nameof(log));
//            _keyStore = keyStore;
//        }

//        public async Task<ResolveResponseDto> ResolveAsync(int accountRootUserPk, int attrId, HttpRequest request, string returnFmt = "json", CancellationToken ct = default)
//        {
//            var evt = new ResolveEventEntity
//            {
//                AccountRootUserPk = accountRootUserPk,
//                AttributeId = attrId,
//                CallerIp = GetCallerIp(request),
//                UserAgent = request.Headers["User-Agent"].ToString(),
//                CreatedAt = DateTime.UtcNow,
//                Outcome = "unknown",
//                CacheHit = false
//            };

//            var result = new ResolveResponseDto
//            {
//                AttributeId = attrId,
//                AccountRootUserPk = accountRootUserPk,
//                CacheHit = false
//            };

//            try
//            {
//                var attr = await _attrRepo.GetByIdAsync(attrId, ct);
//                if (attr == null || string.Equals(attr.Status, "deleted", StringComparison.OrdinalIgnoreCase))
//                {
//                    evt.ResolveUrl = null;
//                    evt.Outcome = "not_found";
//                    await _eventsRepo.AddAsync(evt, ct);
//                    throw new Shortify.Core.Exceptions.NotFoundException("Attribute not found");
//                }

//                if (attr.AccountRootUserPk != accountRootUserPk)
//                {
//                    evt.ResolveUrl = attr.ResolveUrl;
//                    evt.Outcome = "forbidden";
//                    await _eventsRepo.AddAsync(evt, ct);
//                    throw new Shortify.Core.Exceptions.ForbiddenException("Account root mismatch");
//                }

//                if (attr.ExpiryDate.HasValue && attr.ExpiryDate.Value <= DateTime.UtcNow)
//                {
//                    evt.ResolveUrl = attr.ResolveUrl;
//                    evt.Outcome = "expired";
//                    await _eventsRepo.AddAsync(evt, ct);
//                    throw new Shortify.Core.Exceptions.ForbiddenException("Attribute expired");
//                }

//                // Private attributes require auth (placeholder). Keep current behaviour: forbid if private.
//                if (string.Equals(attr.Visibility, "private", StringComparison.OrdinalIgnoreCase))
//                {
//                    evt.ResolveUrl = attr.ResolveUrl;
//                    evt.Outcome = "forbidden";
//                    await _eventsRepo.AddAsync(evt, ct);
//                    throw new Shortify.Core.Exceptions.ForbiddenException("Private attribute");
//                }

//                evt.ResolveUrl = attr.ResolveUrl;

//                if (string.IsNullOrWhiteSpace(attr.ResolveUrl))
//                {
//                    evt.Outcome = "not_found";
//                    await _eventsRepo.AddAsync(evt, ct);
//                    throw new Shortify.Core.Exceptions.NotFoundException("No resolve url configured");
//                }

//                // Build cache key and attempt cache read
//                var key = $"resolve:{accountRootUserPk}:{attr.Id}:{NormalizeUrl(attr.ResolveUrl)}:{returnFmt}";
//                var cachedBytes = await _cache.GetAsync(key, ct);
//                if (cachedBytes != null)
//                {
//                    var cachedStr = Encoding.UTF8.GetString(cachedBytes);
//                    var env = JsonSerializer.Deserialize<CachedEnvelope>(cachedStr);
//                    if (env != null)
//                    {
//                        result.Body = string.IsNullOrEmpty(env.BodyBase64) ? Array.Empty<byte>() : Convert.FromBase64String(env.BodyBase64);
//                        result.ContentType = env.ContentType ?? "application/octet-stream";
//                        result.Signature = env.Signature;
//                        result.RedirectUrl = env.RedirectUrl;
//                        result.CacheHit = true;

//                        evt.CacheHit = true;
//                        evt.Outcome = "success";
//                        await _eventsRepo.AddAsync(evt, ct);
//                        return result;
//                    }
//                }

//                // If attribute configured to redirect, short-circuit with RedirectUrl (no upstream fetch)
//                if (attr.RedirectOnResolve)
//                {
//                    result.RedirectUrl = attr.ResolveUrl;

//                    // cache the redirect envelope
//                    var redirectEnv = new CachedEnvelope
//                    {
//                        BodyBase64 = string.Empty,
//                        ContentType = "text/uri-list",
//                        Signature = null,
//                        RedirectUrl = attr.ResolveUrl
//                    };
//                    var redirectBytes = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(redirectEnv));
//                    var ttl = attr.ResolveCacheTtlSeconds.HasValue ? TimeSpan.FromSeconds(attr.ResolveCacheTtlSeconds.Value) : _defaultTtl;
//                    await _cache.SetAsync(key, redirectBytes, new DistributedCacheEntryOptions().SetAbsoluteExpiration(ttl), ct);

//                    evt.CacheHit = false;
//                    evt.Outcome = "success";
//                    await _eventsRepo.AddAsync(evt, ct);
//                    return result;
//                }

//                // Proxy fetch: forward selected headers and querystring
//                var client = _httpFactory.CreateClient("resolve-fetcher");

//                var upstreamUrl = attr.ResolveUrl;
//                if (request.QueryString.HasValue && !string.IsNullOrEmpty(request.QueryString.Value))
//                {
//                    var qs = request.QueryString.Value!;
//                    upstreamUrl = upstreamUrl.Contains('?') ? upstreamUrl + "&" + qs.TrimStart('?') : upstreamUrl + qs;
//                }

//                using var outgoing = new HttpRequestMessage(HttpMethod.Get, upstreamUrl);

//                // Forward a small, explicit set of headers
//                CopyForwardHeaderIfPresent(request, outgoing.Headers, "Accept");
//                CopyForwardHeaderIfPresent(request, outgoing.Headers, "Accept-Language");
//                CopyForwardHeaderIfPresent(request, outgoing.Headers, "User-Agent");
//                CopyForwardHeaderIfPresent(request, outgoing.Headers, "Referer");
//                // Optionally forward Authorization if you expect upstream to accept same token
//                CopyForwardHeaderIfPresent(request, outgoing.Headers, "Authorization");

//                using var upstreamResp = await client.SendAsync(outgoing, ct);
//                if (!upstreamResp.IsSuccessStatusCode)
//                {
//                    evt.Outcome = "failed";
//                    evt.ErrorMessage = $"Upstream returned {(int)upstreamResp.StatusCode}";
//                    await _eventsRepo.AddAsync(evt, ct);
//                    throw new Exception($"Upstream fetch failed: {(int)upstreamResp.StatusCode}");
//                }

//                var body = await upstreamResp.Content.ReadAsByteArrayAsync(ct);
//                var contentType = upstreamResp.Content.Headers.ContentType?.ToString() ?? "application/octet-stream";

//                result.Body = body;
//                result.ContentType = contentType;

//                // signing (optional)
//                if (attr.ResolveSignedByDefault && _keyStore != null)
//                {
//                    try
//                    {
//                        var signingKey = await _keyStore.GetSigningKeyForTenantAsync(attr.TenantId, ct);
//                        if (!string.IsNullOrEmpty(signingKey))
//                        {
//                            result.Signature = ComputeHmacSha256Base64(body, signingKey);
//                        }
//                    }
//                    catch (Exception ex)
//                    {
//                        _log.LogWarning(ex, "Signing failed for attribute {AttrId}", attr.Id);
//                    }
//                }

//                // Cache the envelope
//                var envelope = new CachedEnvelope
//                {
//                    BodyBase64 = Convert.ToBase64String(result.Body),
//                    ContentType = result.ContentType,
//                    Signature = result.Signature,
//                    RedirectUrl = null
//                };
//                var envBytes = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(envelope));
//                var entryOptions = new DistributedCacheEntryOptions().SetAbsoluteExpiration(attr.ResolveCacheTtlSeconds.HasValue ? TimeSpan.FromSeconds(attr.ResolveCacheTtlSeconds.Value) : _defaultTtl);
//                await _cache.SetAsync(key, envBytes, entryOptions, ct);

//                evt.CacheHit = false;
//                evt.Outcome = "success";
//                await _eventsRepo.AddAsync(evt, ct);

//                return result;
//            }
//            catch
//            {
//                if (evt.Outcome == "unknown")
//                {
//                    evt.Outcome = "failed";
//                    try { await _eventsRepo.AddAsync(evt, ct); } catch { /* swallow logging errors */ }
//                }
//                throw;
//            }
//        }

//        private static string GetCallerIp(HttpRequest req)
//        {
//            if (req.Headers.TryGetValue("X-Forwarded-For", out var v))
//            {
//                return v.ToString();
//            }
//            return req.HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
//        }

//        private static string NormalizeUrl(string url) => url.ToLowerInvariant().Trim();

//        private static string ComputeHmacSha256Base64(byte[] payload, string key)
//        {
//            var keyBytes = Encoding.UTF8.GetBytes(key);
//            using var hmac = new System.Security.Cryptography.HMACSHA256(keyBytes);
//            var sig = hmac.ComputeHash(payload);
//            return Convert.ToBase64String(sig);
//        }

//        private class CachedEnvelope
//        {
//            public string BodyBase64 { get; set; } = string.Empty;
//            public string ContentType { get; set; } = "application/octet-stream";
//            public string? Signature { get; set; }
//            public string? RedirectUrl { get; set; }
//        }

//        private static void CopyForwardHeaderIfPresent(HttpRequest source, HttpRequestHeaders dest, string headerName)
//        {
//            if (source.Headers.TryGetValue(headerName, out var values) && values.Count > 0)
//            {
//                try { dest.Add(headerName, (IEnumerable<string>)values); } catch { /* ignore invalid adds */ }
//            }
//        }
//    }
//}
