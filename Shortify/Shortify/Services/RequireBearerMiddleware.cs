using Microsoft.AspNetCore.Authorization;

namespace Shortify.Services
{
    public class RequireBearerMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<RequireBearerMiddleware> _log;

        public RequireBearerMiddleware(RequestDelegate next, ILogger<RequireBearerMiddleware> log)
        {
            _next = next;
            _log = log;
        }

        public async Task InvokeAsync(HttpContext ctx)
        {
            // If endpoint allows anonymous, skip check
            var endpoint = ctx.GetEndpoint();
            var allowAnonymous = endpoint?.Metadata?.GetMetadata<IAllowAnonymous>() != null;
            if (allowAnonymous)
            {
                await _next(ctx);
                return;
            }

            // Try Authorization header first
            var hasValidHeader = false;
            if (ctx.Request.Headers.TryGetValue("Authorization", out var authHeader) &&
                !string.IsNullOrWhiteSpace(authHeader))
            {
                var header = authHeader.ToString();
                if (header.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
                {
                    // header looks like "Bearer <token>"
                    hasValidHeader = true;
                    ctx.Items["AuthTokenSource"] = "Header";
                }
                else
                {
                    // header present but not Bearer
                    _log.LogDebug("Authorization header present but not Bearer.");
                }
            }

            // If header not valid, try cookie fallback
            var hasCookie = false;
            if (!hasValidHeader)
            {
                if (ctx.Request.Cookies.TryGetValue("AccessToken", out var cookieToken) &&
                    !string.IsNullOrWhiteSpace(cookieToken))
                {
                    hasCookie = true;
                    ctx.Items["AuthTokenSource"] = "Cookie";
                }
            }

            // If neither header nor cookie provided, reject
            if (!hasValidHeader && !hasCookie)
            {
                _log.LogInformation("Request rejected: missing Authorization header and AccessToken cookie. Path: {Path}", ctx.Request.Path);
                ctx.Response.StatusCode = StatusCodes.Status401Unauthorized;
                await ctx.Response.WriteAsync("Missing Authorization header or AccessToken cookie");
                return;
            }

            // If header present but malformed, reject (cookie fallback already handled above)
            if (!hasValidHeader && hasCookie == false && ctx.Request.Headers.ContainsKey("Authorization"))
            {
                // header exists but was malformed and no cookie fallback
                _log.LogInformation("Request rejected: invalid Authorization header format. Path: {Path}", ctx.Request.Path);
                ctx.Response.StatusCode = StatusCodes.Status401Unauthorized;
                await ctx.Response.WriteAsync("Invalid Authorization header");
                return;
            }

            // Continue pipeline; authentication middleware will validate the token (header or cookie)
            await _next(ctx);
        }
    }
}
