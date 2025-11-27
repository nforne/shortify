using System.Security.Claims;

namespace Shortify.Services
{
    public interface IAuthContext
    {
        string? UserId { get; }
        string? TenantId { get; }
        string? Role { get; }
        ClaimsPrincipal? Principal { get; }   // optional, exposes full principal
        ClaimsPrincipal? User { get; }                // optional alias for UserId or username
        IEnumerable<string> Roles { get; }   // optional convenience property
    }

    public class HeaderAuthContext : IAuthContext
    {
        public string? UserId { get; }
        public string? TenantId { get; }
        public ClaimsPrincipal? Principal { get; }
        public ClaimsPrincipal? User { get => Principal; } // alias if you want same name
        string? Role { get; } = string.Empty;
        public IEnumerable<string> Roles { get; }

        string? IAuthContext.Role => Role;

        public HeaderAuthContext(IHttpContextAccessor http, ILogger<HeaderAuthContext> log)
        {
            Principal = http.HttpContext?.User;
            UserId = Principal?.FindFirst(ClaimTypes.NameIdentifier)?.Value
                     ?? Principal?.FindFirst("sub")?.Value;
            TenantId = Principal?.FindFirst("tenant")?.Value
                       ?? Principal?.FindFirst("tenant_id")?.Value;

            Roles = Principal?.FindAll(ClaimTypes.Role).Select(c => c.Value).ToArray()
                    ?? Array.Empty<string>();

            Role = Principal?.FindFirst(ClaimTypes.Role)?.Value ?? "";


            log.LogDebug("HeaderAuthContext created Authenticated={Authenticated} UserId={UserId} TenantId={TenantId}",
                Principal?.Identity?.IsAuthenticated ?? false, UserId, TenantId);
        }
    }


}






//using System.Security.Claims;
//using Microsoft.AspNetCore.Http;
//using Microsoft.Extensions.Primitives;

//namespace Shortify.Services
//{
//    public interface IAuthContext
//    {
//        string? ApiKey { get; }
//        ClaimsPrincipal? User { get; }
//        string? Role { get; }
//        string? TenantId { get; }
//        string? UserId { get; }
//    }

//    // Minimal implementation reading header values via IHttpContextAccessor
//    public class HeaderAuthContext : IAuthContext
//    {
//        private readonly IHttpContextAccessor _http;
//        public HeaderAuthContext(IHttpContextAccessor http) => _http = http;

//        // Header name constants
//        public const string HeaderApiKey = "x-api-key";
//        public const string HeaderRole = "x-role";
//        public const string HeaderTenant = "x-tenant";
//        public const string HeaderUserId = "x-user-id";

//        // Convenience accessor
//        private HttpContext? Http => _http.HttpContext;

//        // Helper to safely read and trim a header value
//        private string? GetHeaderValue(string name)
//        {
//            if (Http?.Request?.Headers == null) return null;
//            if (Http.Request.Headers.TryGetValue(name, out StringValues vals))
//            {
//                var v = vals.FirstOrDefault();
//                return string.IsNullOrWhiteSpace(v) ? null : v.Trim();
//            }
//            return null;
//        }

//        // Prefer claim values, fall back to header
//        public string? ApiKey
//        {
//            get
//            {
//                var user = Http?.User;
//                var apiFromClaims = user?.FindFirst("api_key")?.Value;
//                if (!string.IsNullOrWhiteSpace(apiFromClaims)) return apiFromClaims.Trim();
//                return GetHeaderValue(HeaderApiKey);
//            }
//        }

//        public ClaimsPrincipal? User => Http?.User;

//        // For test flows, support headers: x-role, x-tenant
//        //public string? Role => _http.HttpContext?.Request.Headers["x-role"].FirstOrDefault();
//        public string? Role
//        {
//            get
//            {
//                var user = Http?.User;
//                var roleFromClaims = user?.FindFirst(ClaimTypes.Role)?.Value;
//                if (!string.IsNullOrWhiteSpace(roleFromClaims)) return roleFromClaims.Trim();
//                return GetHeaderValue(HeaderRole);
//            }
//        }

//        // Prefer tenant from claims, then header
//        public string? TenantId
//        {
//            get
//            {
//                var user = Http?.User;
//                var tenantFromClaims = user?.FindFirst("tenant")?.Value;
//                if (!string.IsNullOrWhiteSpace(tenantFromClaims)) return tenantFromClaims.Trim();
//                return GetHeaderValue(HeaderTenant);
//            }
//        }

//        //Name identifier from claims(preferred)
//        //public string? UserId
//        //{
//        //    get
//        //    {
//        //        var user = _http.HttpContext?.User;
//        //        if (user == null) return null;

//        //        // Preferred claim types in order: NameIdentifier, sub, name
//        //        var cid = user.FindFirst(ClaimTypes.NameIdentifier)?.Value
//        //                 ?? user.FindFirst("sub")?.Value
//        //                 ?? user.FindFirst("name")?.Value;

//        //        if (!string.IsNullOrEmpty(cid)) return cid;

//        //        // fallback to header if tests use it
//        //        return _http.HttpContext?.Request.Headers["x-user-id"].FirstOrDefault();
//        //    }
//        //}

//        public string? UserId
//        {
//            get
//            {
//                var user = Http?.User;
//                if (user != null)
//                {
//                    var cid = user.FindFirstValue(ClaimTypes.NameIdentifier)
//                              ?? user.FindFirstValue("sub")
//                              ?? user.FindFirstValue("name");
//                    if (!string.IsNullOrWhiteSpace(cid)) return cid.Trim();
//                }
//                return GetHeaderValue(HeaderUserId);
//            }
//        }
//    }
//}
