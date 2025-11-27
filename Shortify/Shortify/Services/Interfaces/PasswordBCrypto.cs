// File: Auth/Security/BCryptPassword.cs
using System;
using BCrypt.Net;

namespace Shortify.Services.Interfaces
{
    /// <summary>
    /// Simple wrapper around BCrypt.Net-Next for password hashing and verification.
    /// - HashPassword returns the PHC-style bcrypt string (includes salt and cost).
    /// - Verify checks the password against the stored hash.
    /// - GenerateSalt exposes explicit salt generation if you need it.
    /// </summary>
    public static class PasswordBCrypto
    {
        // Recommended default work factor (cost). Increase for stronger hashing (slower).
        // Typical values: 10-14. 12 is a common balance for web apps.
        public const int DefaultWorkFactor = 12;

        /// <summary>
        /// Hash a plain-text password. Returns the bcrypt hash string to store.
        /// </summary>
        /// <param name="password">Plain password (non-null)</param>
        /// <param name="workFactor">BCrypt cost parameter (log2 rounds)</param>
        /// <returns>BCrypt hash string (store this)</returns>
        public static string HashPassword(string password, int workFactor = DefaultWorkFactor)
        {
            if (password is null) throw new ArgumentNullException(nameof(password));
            // BCrypt generates a random salt internally and returns a single string that contains salt+cost+hash.
            return BCrypt.Net.BCrypt.HashPassword(password, workFactor);
        }

        /// <summary>
        /// Verify a plain password against a stored bcrypt hash string.
        /// Returns true if the password matches.
        /// </summary>
        public static bool Verify(string password, string storedHash)
        {
            if (password is null) throw new ArgumentNullException(nameof(password));
            if (storedHash is null) throw new ArgumentNullException(nameof(storedHash));

            // BCrypt.Net.Verify performs a safe verification; it handles salt and cost embedded in storedHash.
            return BCrypt.Net.BCrypt.Verify(password, storedHash);
        }

        /// <summary>
        /// Generate a salt string with an explicit work factor (rarely needed because HashPassword does this).
        /// You can store this salt separately if you prefer, but bcrypt embeds it in the hash by default.
        /// </summary>
        public static string GenerateSalt(int workFactor = DefaultWorkFactor)
        {
            return BCrypt.Net.BCrypt.GenerateSalt(workFactor);
        }
    }
}
