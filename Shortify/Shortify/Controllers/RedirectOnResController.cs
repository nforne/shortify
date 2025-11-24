//using System.Text;
//using System.Text.Json;
//using Microsoft.AspNetCore.Mvc;
//using Shortify.Core.Contracts;
//using Shortify.Services.Interfaces;

//namespace Shortify.Controllers
//{
//    [ApiController]  // uncomment to make it available.
//    [Route("stfy")]
//    public class RedirectOnResController : ControllerBase
//    {
//        private readonly IResolveService _svc;
//        public RedirectOnResController(IResolveService svc) => _svc = svc;

//        [HttpGet("{root:int}/{attr:int}")]  // uncomment to make it available.
//        public async Task<IActionResult> Resolve(int root, int attr, [FromQuery] string? @return = "json", CancellationToken ct = default)
//        {
//            try
//            {
//                // The ResolveAsync result now includes a RedirectUrl when RedirectOnResolve=true
//                var resp = await _svc.ResolveAsync(root, attr, Request, @return ?? "json", ct);

//                if (!string.IsNullOrEmpty(resp.RedirectUrl))
//                {
//                    // Preserve original query string if upstream expects it
//                    var redirectUrl = resp.RedirectUrl;
//                    if (Request.QueryString.HasValue)
//                    {
//                        var qs = Request.QueryString.Value!;
//                        redirectUrl = redirectUrl.Contains('?') ? redirectUrl + "&" + qs.TrimStart('?') : redirectUrl + qs;
//                    }

//                    // Use 302 Found for now; can be changed to 307/308 if you want to preserve method semantics
//                    return Redirect(redirectUrl);
//                }

//                if ((@return ?? "json").Equals("raw", StringComparison.OrdinalIgnoreCase))
//                {
//                    return File(resp.Body, resp.ContentType);
//                }

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
//                return Convert.ToBase64String(bytes);
//            }
//        }
//    }
//}
