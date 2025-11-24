//using System.Text;
//using System.Text.Json;
//using Microsoft.AspNetCore.Authorization;
//using Microsoft.AspNetCore.Mvc;
//using Shortify.Services.Interfaces;

//namespace Shortify.ScatchPlus
//{
//    [ApiController]
//    [Route("stfy")]
//    public class ResolveController : ControllerBase
//    {
//        private readonly IResolveService _svc;
//        public ResolveController(IResolveService svc) => _svc = svc;

//        //[AllowAnonymous] // <- add this
//        [HttpGet("{root:int}/{attr:int}")]
//        public async Task<IActionResult> Resolve(int root, int attr, [FromQuery] string? @return = "json", CancellationToken ct = default)
//        {
//            try
//            {
//                var resp = await _svc.ResolveAsync(root, attr, Request, @return ?? "json", ct);

//                if ((@return ?? "json").Equals("raw", StringComparison.OrdinalIgnoreCase))
//                {
//                    // return raw body with upstream content type
//                    return File(resp.Body, resp.ContentType);
//                }

//                // wrap in DTO for JSON responses
//                var wrapper = new
//                {
//                    attributeId = resp.AttributeId,
//                    accountRootUserPk = resp.AccountRootUserPk,
//                    contentType = resp.ContentType,
//                    signature = resp.Signature,
//                    cacheHit = resp.CacheHit,
//                    payload = TryDeserializeJson(resp.Body)
//                };

//                return Ok(wrapper);
//            }
//            catch (Core.Exceptions.NotFoundException)
//            {
//                return NotFound();
//            }
//            catch (Core.Exceptions.ForbiddenException)
//            {
//                return Forbid();
//            }
//            catch (Exception)
//            {
//                // generic server error for upstream failures
//                return StatusCode(502);
//            }
//        }

//        private static object TryDeserializeJson(byte[] bytes)
//        {
//            try
//            {
//                var s = Encoding.UTF8.GetString(bytes);
//                return JsonSerializer.Deserialize<JsonElement>(s);
//            }
//            catch
//            {
//                // return base64 string fallback
//                return Convert.ToBase64String(bytes);
//            }
//        }
//    }
//}
