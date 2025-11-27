using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Shortify.DTOs.AuthOptionDTOs
{
    // -------------------------
    // DTOs used by AuthController
    // -------------------------

    /// <summary>
    /// DTO for tenant signup. At least one of DisplayName, FirstName or LastName must be provided.
    /// If DisplayName is not provided, the service may fall back to FirstName then LastName.
    /// </summary>
    public class SignUpDto : IValidatableObject
    {
        [Required]
        public string TenantName { get; set; } = "";

        [Required]
        [EmailAddress]
        public string AdminEmail { get; set; } = "";

        [Required]
        [MinLength(8)]
        public string AdminPassword { get; set; } = "";

        [MaxLength(200)]
        public string? DisplayName { get; set; }

        [MaxLength(100)]
        public string? FirstName { get; set; }

        [MaxLength(100)]
        public string? LastName { get; set; }

        /// <summary>
        /// Ensure at least one of DisplayName, FirstName or LastName is provided.
        /// </summary>
        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            var hasDisplay = !string.IsNullOrWhiteSpace(DisplayName);
            var hasFirst = !string.IsNullOrWhiteSpace(FirstName);
            var hasLast = !string.IsNullOrWhiteSpace(LastName);

            if (!hasDisplay && !hasFirst && !hasLast)
            {
                yield return new ValidationResult(
                    "At least one of DisplayName, FirstName or LastName must be provided.",
                    new[] { nameof(DisplayName), nameof(FirstName), nameof(LastName) }
                );
            }

            // Optional: additional name validation rules can be added here (e.g., disallow numeric-only names).
            yield break;
        }
    }

    public class SignInDto
    {
        [Required]
        [EmailAddress]
        public string Email { get; set; } = "";

        [Required]
        public string Password { get; set; } = "";
    }

    public class TestTokenDto
    {
        public string TenantId { get; set; } = "dev";
        public string Email { get; set; } = "dev@example.com";
        public List<string>? Roles { get; set; }
    }

    public class AuthTokenDto
    {
        public string AccessToken { get; set; } = "";
        public string TokenType { get; set; } = "Bearer";
    }

    public class SignUpResultDto
    {
        public string TenantId { get; set; } = "";
        public int AdminUserId { get; set; }
        public string Email { get; set; } = "";
    }
}
