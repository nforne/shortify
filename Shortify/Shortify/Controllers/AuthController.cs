// File: Controllers/AuthController.cs
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Shortify.DTOs.AuthOptionDTOs;
using Shortify.Models.AuthOptions;
using Shortify.Services.Interfaces;

namespace Shortify.Controllers
{
    [ApiController]
    [Route("api/auth")]
    public class AuthController : ControllerBase
    {
        private readonly IAuthService _auth;
        private readonly AuthOptions _opts;

        public AuthController(IAuthService auth, IOptions<AuthOptions> opts)
        {
            _auth = auth;
            _opts = opts.Value;
        }

        /// <summary>
        /// Tenant signup: creates a tenant root user (IsTenant = true) and returns tenant info.
        /// </summary>
        //[HttpPost("signup")]
        //public async Task<IActionResult> SignUp([FromBody] SignUpDto dto, CancellationToken ct = default)
        //{
        //    if (!ModelState.IsValid) return BadRequest(ModelState);

        //    // Create tenant root user
        //    var created = await _auth.SignUpTenantAsync(dto.TenantName, dto.AdminEmail, dto.AdminPassword, ct);

        //    var result = new SignUpResultDto
        //    {
        //        TenantId = created.TenantId,
        //        AdminUserId = created.Id,
        //        Email = created.Email
        //    };

        //    return CreatedAtAction(nameof(SignUp), result);
        //}

        [AllowAnonymous]
        [HttpPost("signup")]
        public async Task<IActionResult> SignUp([FromBody] SignUpDto dto, CancellationToken ct = default)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            // Pass optional name fields from DTO into the service
            var created = await _auth.SignUpTenantAsync(
                dto.TenantName,
                dto.AdminEmail,
                dto.AdminPassword,
                dto.DisplayName,
                dto.FirstName,
                dto.LastName,
                ct
            );

            var result = new SignUpResultDto
            {
                TenantId = created.TenantId,
                AdminUserId = created.Id,
                Email = created.Email
            };

            return CreatedAtAction(nameof(SignUp), result);
        }


        /// <summary>
        /// Sign in: shared route for all users. Returns JWT access token on success.
        /// Email is unique across the system so tenant id is not required.
        /// </summary>
        [AllowAnonymous]
        [HttpPost("signin")]
        public async Task<IActionResult> SignIn([FromBody] SignInDto dto, CancellationToken ct = default)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);
            var token = await _auth.SignInAsync(dto.Email, dto.Password, ct);            
            if (token == null) return Unauthorized(new { message = "Invalid credentials" });
            return Ok(new AuthTokenDto { AccessToken = token, TokenType = "Bearer" });
        }

        /// <summary>
        /// Create a test token. Allowed only when PermissiveMode or DisableAuth is enabled.
        /// </summary>
        
        [AllowAnonymous]
        [HttpPost("test-token")]
        public async Task<IActionResult> TestToken([FromBody] TestTokenDto dto, CancellationToken ct = default)
        {
            if (!_opts.PermissiveMode && !_opts.DisableAuth) return Forbid();

            var token = await _auth.CreateTestTokenAsync(dto.TenantId, dto.Email, dto.Roles, ct);
            return Ok(new AuthTokenDto { AccessToken = token, TokenType = "Bearer" });
        }
    }
    
}

