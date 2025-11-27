using System;

namespace Shortify.Models
{
    public class UserEntity
    {
        public int Id { get; set; }

        public string TenantId { get; set; } = default!;

        public string Email { get; set; } = default!;

        public bool IsTenant { get; set; } = false;

        // First and last name added
        public string? FirstName { get; set; }
        public string? LastName { get; set; }

        // Backing field for DisplayName so EF can map it, but getter falls back to names when not set.
        private string? _displayName;
        public string DisplayName
        {
            get
            {
                if (!string.IsNullOrWhiteSpace(_displayName))
                    return _displayName!;
                if (!string.IsNullOrWhiteSpace(FirstName))
                    return FirstName!;
                if (!string.IsNullOrWhiteSpace(LastName))
                    return LastName!;
                return string.Empty;
            }
            set => _displayName = value;
        }

        public string RolesJson { get; set; } = "[]";

        public string Status { get; set; } = "active";

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? UpdatedAt { get; set; }

        public DateTime? DeletedAt { get; set; }

        // New authentication fields
        public string? PasswordHash { get; set; }    // PBKDF2 hash (base64) or other encoded hash
        public string? PasswordSalt { get; set; }    // random salt (base64)
        public DateTime? PasswordChangedAt { get; set; } // optional audit

        /// <summary>
        /// Ensure the entity has at least one of FirstName, LastName, or DisplayName set.
        /// Call this before creating/updating the user to enforce the rule.
        /// Throws ArgumentException if none are provided.
        /// </summary>
        public void EnsureHasName()
        {
            var hasDisplay = !string.IsNullOrWhiteSpace(_displayName);
            var hasFirst = !string.IsNullOrWhiteSpace(FirstName);
            var hasLast = !string.IsNullOrWhiteSpace(LastName);

            if (!hasDisplay && !hasFirst && !hasLast)
                throw new ArgumentException("At least one of DisplayName, FirstName or LastName must be provided.");
        }

        /// <summary>
        /// Convenience: set DisplayName from names if not explicitly provided.
        /// Call this before persisting if you want DisplayName to be populated.
        /// </summary>
        public void PopulateDisplayNameIfMissing()
        {
            if (!string.IsNullOrWhiteSpace(_displayName)) return;

            if (!string.IsNullOrWhiteSpace(FirstName))
                _displayName = FirstName;
            else if (!string.IsNullOrWhiteSpace(LastName))
                _displayName = LastName;
        }
    }
}
