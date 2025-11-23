using System.Security.Claims;

namespace Shortify.Services
{
    public interface IAuthContext
    {
        string? ApiKey { get; }
        ClaimsPrincipal? User { get; }
        string? Role { get; }
        string? TenantId { get; }

        // Add this property
        string? UserId { get; }
    }

    // Minimal implementation reading header values via IHttpContextAccessor
    public class HeaderAuthContext : IAuthContext
    {
        private readonly IHttpContextAccessor _http;
        public HeaderAuthContext(IHttpContextAccessor http) => _http = http;

        public string? ApiKey => _http.HttpContext?.Request.Headers["x-api-key"].FirstOrDefault();

        public ClaimsPrincipal? User => _http.HttpContext?.User;

        // For test flows, support headers: x-role, x-tenant
        public string? Role => _http.HttpContext?.Request.Headers["x-role"].FirstOrDefault();

        public string? TenantId => _http.HttpContext?.Request.Headers["x-tenant"].FirstOrDefault();

        //Name identifier from claims(preferred)
        public string? UserId
        {
            get
            {
                var user = _http.HttpContext?.User;
                if (user == null) return null;

                // Preferred claim types in order: NameIdentifier, sub, name
                var cid = user.FindFirst(ClaimTypes.NameIdentifier)?.Value
                         ?? user.FindFirst("sub")?.Value
                         ?? user.FindFirst("name")?.Value;

                if (!string.IsNullOrEmpty(cid)) return cid;

                // fallback to header if tests use it
                return _http.HttpContext?.Request.Headers["x-user-id"].FirstOrDefault();
            }
        }
    }
}
