using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Shortify.Repositories.Interfaces;

[ApiController]
[Route("stfy")]
public class PublicResolveController : ControllerBase
{
    private readonly IAttributeRepository _attrRepo;

    public PublicResolveController(IAttributeRepository attrRepo)
    {
        _attrRepo = attrRepo;
    }

    [AllowAnonymous]
    [HttpGet("{root:int}/{attr:int}")]
    public async Task<IActionResult> Resolve(int root, int attr, CancellationToken ct = default)
    {
        var attrb = await _attrRepo.GetByIdAsync(attr, ct);
        if (attrb == null || string.Equals(attrb.Status, "deleted", StringComparison.OrdinalIgnoreCase))
            return NotFound();

        if (attrb.Visibility == "private" && attrb.AccountRootUserPk != root)
            return Forbid();

        if (attrb.ExpiryDate.HasValue && attrb.ExpiryDate.Value <= DateTime.UtcNow)
            return Forbid();

        if (string.IsNullOrWhiteSpace(attrb.ResolveUrl))
            return NotFound();

        // Simple policy: allow public attributes; if private you can keep Forbid() above.
        // Normalize URL: if no scheme, assume https
        var target = EnsureAbsoluteUrl(attrb.ResolveUrl);

        // 302 Found redirect. Use RedirectPermanent (301) or RedirectPreserveMethod (307/308) if you prefer.
        return Redirect(target);
    }

    private static string EnsureAbsoluteUrl(string url)
    {
        if (Uri.TryCreate(url, UriKind.Absolute, out var uri) && !string.IsNullOrWhiteSpace(uri.Scheme))
            return uri.ToString();

        // If it's missing a scheme, default to https
        if (Uri.TryCreate("https://" + url, UriKind.Absolute, out var withScheme))
            return withScheme.ToString();

        // As a last resort, return the original string so Redirect() will still attempt it
        return url;
    }
}
