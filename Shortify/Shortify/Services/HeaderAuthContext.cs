using System.Security.Claims;

namespace Shortify.Services
{
    public interface IAuthContext
    {
        string? ApiKey { get; }
        ClaimsPrincipal? User { get; }
        string? Role { get; }
        string? TenantId { get; }
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
    }
}
