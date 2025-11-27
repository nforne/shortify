using System.Net;
using System.Text.Json;

namespace Shortify.ScatchPlus
{
    public class ExceptionMappingMiddleware
    {
        private readonly RequestDelegate _next;
        public ExceptionMappingMiddleware(RequestDelegate next) => _next = next;

        public async Task Invoke(HttpContext ctx)
        {
            try
            {
                await _next(ctx);
            }
            catch (UnauthorizedAccessException ex)
            {
                ctx.Response.StatusCode = (int)HttpStatusCode.Forbidden;
                ctx.Response.ContentType = "application/json";
                await ctx.Response.WriteAsync(JsonSerializer.Serialize(new { error = ex.Message }));
            }
            catch (InvalidOperationException ex)
            {
                ctx.Response.StatusCode = (int)HttpStatusCode.Conflict;
                ctx.Response.ContentType = "application/json";
                await ctx.Response.WriteAsync(JsonSerializer.Serialize(new { error = ex.Message }));
            }
            catch (ArgumentException ex)
            {
                ctx.Response.StatusCode = (int)HttpStatusCode.BadRequest;
                ctx.Response.ContentType = "application/json";
                await ctx.Response.WriteAsync(JsonSerializer.Serialize(new { error = ex.Message }));
            }
            catch (Exception ex)
            {
                ctx.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
                ctx.Response.ContentType = "application/json";
                await ctx.Response.WriteAsync(JsonSerializer.Serialize(new { error = "internal server error" }));
            }
        }
    }

    public static class ExceptionMappingMiddlewareExtensions
    {
        public static IApplicationBuilder UseExceptionMapping(this IApplicationBuilder app) =>
            app.UseMiddleware<ExceptionMappingMiddleware>();
    }
}
