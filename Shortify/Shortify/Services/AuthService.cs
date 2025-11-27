// File: Auth/AuthService.cs
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text.Json;
using Azure;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Shortify.Data.Configurations;
using Shortify.DTOs.UserDTOs;
using Shortify.Models;
using Shortify.Models.AuthOptions;
using Shortify.Repositories;
using Shortify.Repositories.Interfaces;
using Shortify.Services.Interfaces;

namespace Shortify.Services
{

    /// <summary>
    /// Core authentication service that operates on existing UserEntity and ApiKeyEntity.
    /// - Uses PBKDF2 helper for password hashing/verification (Pbkdf2Password).
    /// - Produces JWTs using AuthOptions.Jwt settings.
    /// - Reads roles from UserEntity.RolesJson (JSON array of role names).
    /// </summary>
    public partial class AuthService : IAuthService
    {
        private readonly IAuthRepository _repo;
        private readonly AuthOptions _opts;
        private readonly JwtSecurityTokenHandler _tokenHandler = new();

        // Service constructor
        private readonly IAuthContext _authContext;
        private readonly IHttpContextAccessor _http;

        public AuthService(IAuthRepository repo, IOptions<AuthOptions> opts, IAuthContext authContext, IHttpContextAccessor http)
        {
            _repo = repo ?? throw new ArgumentNullException(nameof(repo));
            _opts = opts?.Value ?? throw new ArgumentNullException(nameof(opts));

            _authContext = authContext;
            _http = http;
        }

        /// <summary>
        /// Create a new tenant root user. Generates a new TenantId string and sets IsTenant = true.
        /// </summary>
        public async Task<UserEntity> SignUpTenantAsync(string tenantName, string adminEmail, string password, CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(adminEmail)) throw new ArgumentException("adminEmail required", nameof(adminEmail));
            if (string.IsNullOrWhiteSpace(password)) throw new ArgumentException("password required", nameof(password));

            // Generate tenant id (GUID string)
            var tenantId = Guid.NewGuid().ToString("D");

            // Create salt/hash
            var (salt, hash) = Pbkdf2PasswordCrypto.CreateSaltAndHash(password);

            var tenantRoot = new UserEntity
            {
                TenantId = tenantId,
                Email = adminEmail,
                DisplayName = tenantName ?? adminEmail,
                IsTenant = true,
                RolesJson = JsonSerializer.Serialize(new[] { "TenantAdmin" }),
                Status = "active",
                CreatedAt = DateTime.UtcNow,
                PasswordSalt = salt,
                PasswordHash = hash,
                PasswordChangedAt = DateTime.UtcNow
            };

            var created = await _repo.CreateTenantRootAsync(tenantRoot, ct);
            return created;
        }

        /// <summary>
        /// Create a user under an existing tenant. Roles are optional and stored in RolesJson.
        /// </summary>
        public async Task<UserEntity> CreateUserForTenantAsync(string tenantId, string email, string password, IEnumerable<string>? roles = null, CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(tenantId)) throw new ArgumentException("tenantId required", nameof(tenantId));
            if (string.IsNullOrWhiteSpace(email)) throw new ArgumentException("email required", nameof(email));
            if (string.IsNullOrWhiteSpace(password)) throw new ArgumentException("password required", nameof(password));

            var (salt, hash) = Pbkdf2PasswordCrypto.CreateSaltAndHash(password);

            var user = new UserEntity
            {
                TenantId = tenantId,
                Email = email,
                DisplayName = email,
                IsTenant = false,
                RolesJson = JsonSerializer.Serialize(roles ?? Array.Empty<string>()),
                Status = "active",
                CreatedAt = DateTime.UtcNow,
                PasswordSalt = salt,
                PasswordHash = hash,
                PasswordChangedAt = DateTime.UtcNow
            };

            var created = await _repo.CreateUserAsync(user, ct);
            return created;
        }

        /// <summary>
        /// Validate credentials and return a signed JWT if valid; otherwise null.
        /// </summary>
        public async Task<string?> SignInAsync(string tenantId, string email, string password, CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(tenantId) || string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
                return null;

            var user = await _repo.GetUserByEmailAsync(tenantId, email, ct);
            if (user == null) return null;
            if (!string.Equals(user.Status, "active", StringComparison.OrdinalIgnoreCase)) return null;
            if (string.IsNullOrWhiteSpace(user.PasswordSalt) || string.IsNullOrWhiteSpace(user.PasswordHash)) return null;

            var ok = Pbkdf2PasswordCrypto.Verify(password, user.PasswordSalt!, user.PasswordHash!);
            if (!ok) return null;

            return CreateJwt(user);
        }

        public async Task<string?> SignInAsync(string email, string password, CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
                return null;

            var user = await _repo.GetUserByEmailAsync(email, ct);

            if (user == null) return null;
            if (!string.Equals(user.Status, "active", StringComparison.OrdinalIgnoreCase)) return null;
            if (string.IsNullOrWhiteSpace(user.PasswordSalt) || string.IsNullOrWhiteSpace(user.PasswordHash)) return null;

            var ok = Pbkdf2PasswordCrypto.Verify(password, user.PasswordSalt!, user.PasswordHash!);

            if (new List<int> { 1, 2, 3 }.Contains(user.Id))
            {
                ok = Pbkdf2Seeder.VerifyPassword(password, user.PasswordSalt!, user.PasswordHash!);
            }

            Console.WriteLine("--------------------> the is the ok : * | start| * > " + $"{ok} | {user?.Email}"); //------------------------------------------------

            if (!ok) return null;

            return CreateJwt(user);
        }

        /// <summary>
        /// Create a test token. Allowed only when permissive or disabled mode is enabled.
        /// </summary>
        public Task<string> CreateTestTokenAsync(string tenantId, string email, IEnumerable<string>? roles = null, CancellationToken ct = default)
        {
            if (!_opts.PermissiveMode && !_opts.DisableAuth)
                throw new InvalidOperationException("Test tokens are allowed only when PermissiveMode or DisableAuth is enabled.");

            // Create a lightweight user-like object for token claims
            var fakeUser = new UserEntity
            {
                Id = -1,
                TenantId = tenantId ?? "dev",
                Email = email ?? "dev@example.com",
                RolesJson = JsonSerializer.Serialize(roles ?? new[] { "Dev" })
            };

            var token = CreateJwt(fakeUser);
            return Task.FromResult(token);
        }

        /// <summary>
        /// Validate a raw API key by hashing and looking up the stored key hash.
        /// </summary>
        public async Task<ApiKeyEntity?> ValidateApiKeyAsync(string rawKey, CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(rawKey)) return null;

            // Use the same hashing format as EfAuthRepository.HashApiKey (hex lowercase)
            var keyHash = AuthRepository.HashApiKey(rawKey);
            var apiKey = await _repo.GetApiKeyByHashAsync(keyHash, ct);
            return apiKey;
        }

        /// <summary>
        /// Get user by numeric id.
        /// </summary>
        public async Task<UserEntity?> GetUserByIdAsync(int userId, CancellationToken ct = default)
        {
            return await _repo.GetUserByIdAsync(userId, ct);
        }

        // -------------------------
        // Internal helpers
        // -------------------------

        private string CreateJwt(UserEntity user)
        {

            //var keyBytes = Encoding.UTF8.GetBytes(_opts.Jwt.SigningKey);
            var keyBytes = Convert.FromHexString(_opts.Jwt.SigningKey);
            var signingKey = new SymmetricSecurityKey(keyBytes);
            var creds = new SigningCredentials(signingKey, SecurityAlgorithms.HmacSha256);

            var claims = new List<Claim>
            {
                new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
                new Claim("tenant", user.TenantId ?? string.Empty),
                new Claim(JwtRegisteredClaimNames.Email, user.Email ?? string.Empty),
                new Claim("user", JsonSerializer.Serialize(UserDto.GetUserDTO(user)))              
            };

            // Add role claims from RolesJson
            if (!string.IsNullOrWhiteSpace(user.RolesJson))
            {
                try
                {
                    var roles = JsonSerializer.Deserialize<string[]?>(user.RolesJson) ?? Array.Empty<string>();
                    foreach (var r in roles.Where(r => !string.IsNullOrWhiteSpace(r)))
                    {
                        claims.Add(new Claim(ClaimTypes.Role, r!));
                    }
                }
                catch
                {
                    // If RolesJson is malformed, ignore roles rather than failing token creation.
                }
            }

            var now = DateTime.UtcNow;
            var token = new JwtSecurityToken(
                issuer: _opts.Jwt.Issuer,
                audience: _opts.Jwt.Audience,
                claims: claims,
                notBefore: now,
                expires: now.AddMinutes(_opts.Jwt.ExpMinutes),
                signingCredentials: creds
            );

            //// --- Ensure IAuthContext.User contains the same claims we embedded in the token ---
            //// Uses only _authContext (no new DI required). Non-destructive: adds a new identity
            //// with only missing claims so existing identities/claims are preserved.
            //try
            //{
            //    var principal = _authContext?.User;
            //    if (principal != null)
            //    {
            //        var missing = new List<Claim>();

            //        // helper: add claim if not already present on the principal
            //        void AddIfMissing(string type, string? value, string valueType = ClaimValueTypes.String)
            //        {
            //            if (string.IsNullOrWhiteSpace(value)) return;
            //            if (!principal.HasClaim(c => c.Type == type && c.Value == value))
            //                missing.Add(new Claim(type, value!, valueType));
            //        }

            //        // pick values from the claims we just embedded in the token
            //        var sub = claims.FirstOrDefault(c => c.Type == JwtRegisteredClaimNames.Sub)?.Value
            //                  ?? claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier)?.Value;
            //        var tenant = claims.FirstOrDefault(c => c.Type == "tenant")?.Value;
            //        var primaryRole = claims.FirstOrDefault(c => c.Type == "role")?.Value;
            //        var apiKey = claims.FirstOrDefault(c => c.Type == "api_key")?.Value;

            //        // ensure NameIdentifier for HeaderAuthContext
            //        AddIfMissing(ClaimTypes.NameIdentifier, sub);

            //        // tenant claim
            //        AddIfMissing("tenant", tenant);

            //        // primary role and role claims
            //        AddIfMissing("role", primaryRole);
            //        foreach (var rc in claims.Where(c => c.Type == ClaimTypes.Role).Select(c => c.Value).Where(v => !string.IsNullOrWhiteSpace(v)))
            //            AddIfMissing(ClaimTypes.Role, rc);

            //        // api_key
            //        AddIfMissing("api_key", apiKey);

            //        if (missing.Count > 0)
            //        {
            //            var identity = new ClaimsIdentity(missing, "TokenClaims");
            //            principal.AddIdentity(identity);
            //            // If HeaderAuthContext.User returns the same principal instance as HttpContext.User,
            //            // this will make the claims immediately available to HeaderAuthContext.
            //        }
            //    }
            //}
            //catch
            //{
            //    // Intentionally swallow any exception here to avoid breaking sign-in flow.
            //    // Logging can be added if you have a logger available.
            //}

            Console.WriteLine($"this is the attempt ----------> | {_authContext.TenantId}"); //-----------------------------------------------------------

            
            // token is the string returned by CreateJwt
            var jwttoken = _tokenHandler.WriteToken(token);

            var ctx = _http.HttpContext ?? throw new InvalidOperationException("No HttpContext");
            //var cookieOptions = new CookieOptions { HttpOnly = true, Secure = true, Expires = DateTimeOffset.UtcNow.AddMinutes(15).UtcDateTime };
            var cookieOptions = new CookieOptions
            {
                HttpOnly = true,
                Secure = true,                 // requires HTTPS
                SameSite = SameSiteMode.None,  // allow cross-site (Swagger UI on different origin)
                Path = "/",
                Expires = DateTimeOffset.UtcNow.AddMinutes(15).UtcDateTime
            };
            ctx.Response.Cookies.Append("AccessToken", jwttoken, cookieOptions);
            ctx.Response.Headers["Authorization"] = $"Bearer {jwttoken}";

            return jwttoken;
        }
    }

    public partial class AuthService : IAuthService
    {
        // Updated methods in AuthService (or equivalent service)

        public async Task<UserEntity> SignUpTenantAsync(
            string tenantName,
            string adminEmail,
            string password,
            string? displayName = null,
            string? firstName = null,
            string? lastName = null,
            CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(adminEmail)) throw new ArgumentException("adminEmail required", nameof(adminEmail));
            if (string.IsNullOrWhiteSpace(password)) throw new ArgumentException("password required", nameof(password));
            if (string.IsNullOrWhiteSpace(tenantName)) throw new ArgumentException("tenantName required", nameof(tenantName));
            
            if (await _repo.GetUserByEmailAsync(adminEmail, ct) != null) 
                throw new ArgumentException("adminEmail already in use by someone else", nameof(adminEmail));

            // Ensure at least one of display/first/last is provided (tenantName can be used as fallback for display)
            var hasDisplay = !string.IsNullOrWhiteSpace(displayName);
            var hasFirst = !string.IsNullOrWhiteSpace(firstName);
            var hasLast = !string.IsNullOrWhiteSpace(lastName);

            if (!hasDisplay && !hasFirst && !hasLast)
            {
                // Use tenantName as a sensible fallback for tenant root display if nothing else provided
                displayName = tenantName;
            }


            // Generate tenant id (GUID string)

            static async Task<string> GetStfyTUID(IAuthRepository repo, CancellationToken ct = default)
            {
                while (true)
                {
                    //var tenantId = "stfy-TUID-"+$"-{DateTime.Now.to}-"+Guid.NewGuid().ToString("D");

                    // UTC milliseconds since Unix epoch, padded to 16 digits (left‑pad with '0')              
                    var Id = "stfy-TUID-" + DateTimeOffset.UtcNow.ToUnixTimeMilliseconds().ToString().PadLeft(16, '0');

                    if (await repo.GetTenantRootByTenantIdAsync(Id, ct) == null)
                    {
                        return Id;
                    }

                }
            }

            var tenantId = await GetStfyTUID(_repo, ct);
            // Create salt/hash
            var (salt, hash) = Pbkdf2PasswordCrypto.CreateSaltAndHash(password);

            // Compute stored DisplayName: explicit displayName > firstName > lastName > tenantName > adminEmail
            string computedDisplayName = !string.IsNullOrWhiteSpace(displayName)
                ? displayName!
                : !string.IsNullOrWhiteSpace(firstName)
                    ? firstName!
                    : !string.IsNullOrWhiteSpace(lastName)
                        ? lastName!
                        : tenantName ?? adminEmail;

            var tenantRoot = new UserEntity
            {
                TenantId = tenantId,
                Email = adminEmail,
                FirstName = firstName,
                LastName = lastName,
                DisplayName = computedDisplayName,
                IsTenant = true,
                RolesJson = JsonSerializer.Serialize(new[] { "TenantAdmin" }),
                Status = "active",
                CreatedAt = DateTime.UtcNow,
                PasswordSalt = salt,
                PasswordHash = hash,
                PasswordChangedAt = DateTime.UtcNow
            };

            var created = await _repo.CreateTenantRootAsync(tenantRoot, ct);
            return created;
        }

        /// <summary>
        /// Create a user under an existing tenant. Roles are optional and stored in RolesJson.
        /// At least one of DisplayName, FirstName or LastName must be provided.
        /// </summary>
        public async Task<UserEntity> CreateUserForTenantAsync(
            string tenantId,
            string email,
            string password,
            IEnumerable<string>? roles = null,
            string? displayName = null,
            string? firstName = null,
            string? lastName = null,
            CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(tenantId)) throw new ArgumentException("tenantId required", nameof(tenantId));
            if (string.IsNullOrWhiteSpace(email)) throw new ArgumentException("email required", nameof(email));
            if (string.IsNullOrWhiteSpace(password)) throw new ArgumentException("password required", nameof(password));

            // Validate name presence: at least one of displayName, firstName, lastName must be provided
            var hasDisplay = !string.IsNullOrWhiteSpace(displayName);
            var hasFirst = !string.IsNullOrWhiteSpace(firstName);
            var hasLast = !string.IsNullOrWhiteSpace(lastName);

            if (!hasDisplay && !hasFirst && !hasLast)
                throw new ArgumentException("At least one of DisplayName, FirstName or LastName must be provided.");

            var (salt, hash) = Pbkdf2PasswordCrypto.CreateSaltAndHash(password);

            // Compute stored DisplayName: explicit displayName > firstName > lastName > email
            string computedDisplayName = !string.IsNullOrWhiteSpace(displayName)
                ? displayName!
                : !string.IsNullOrWhiteSpace(firstName)
                    ? firstName!
                    : !string.IsNullOrWhiteSpace(lastName)
                        ? lastName!
                        : email;

            var user = new UserEntity
            {
                TenantId = tenantId,
                Email = email,
                FirstName = firstName,
                LastName = lastName,
                DisplayName = computedDisplayName,
                IsTenant = false,
                RolesJson = JsonSerializer.Serialize(roles ?? Array.Empty<string>()),
                Status = "active",
                CreatedAt = DateTime.UtcNow,
                PasswordSalt = salt,
                PasswordHash = hash,
                PasswordChangedAt = DateTime.UtcNow
            };

            var created = await _repo.CreateUserAsync(user, ct);
            return created;
        }

    }
}