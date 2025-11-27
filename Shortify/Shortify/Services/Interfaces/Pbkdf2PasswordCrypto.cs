// File: Auth/Security/Pbkdf2Password.cs
using System;
using System.Security.Cryptography;
using System.Text;

namespace Shortify.Services.Interfaces
{
    /// <summary>
    /// PBKDF2 helper that matches the seeding parameters used in UserEntityConfiguration.
    /// - Salt is returned as Base64
    /// - Hash is returned as Base64
    /// - Verification uses constant-time comparison
    /// </summary>
    public static class Pbkdf2PasswordCrypto
    {
        private const int SaltSize = 16;      // bytes (128-bit)
        private const int KeySize = 32;       // bytes (256-bit)
        private const int Iterations = 100_000;
        private static readonly HashAlgorithmName HashAlgorithm = HashAlgorithmName.SHA256;

        /// <summary>
        /// Create a cryptographically-random salt and return it as a Base64 string.
        /// </summary>
        public static string CreateSalt()
        {
            var salt = new byte[SaltSize];
            using var rng = RandomNumberGenerator.Create();
            rng.GetBytes(salt);
            return Convert.ToBase64String(salt);
        }

        /// <summary>
        /// Hash a password with the provided Base64 salt and return the derived key as Base64.
        /// </summary>
        public static string HashPassword(string password, string saltBase64)
        {
            if (password == null) throw new ArgumentNullException(nameof(password));
            if (saltBase64 == null) throw new ArgumentNullException(nameof(saltBase64));

            var salt = Convert.FromBase64String(saltBase64);
            using var derive = new Rfc2898DeriveBytes(password, salt, Iterations, HashAlgorithm);
            var key = derive.GetBytes(KeySize);
            return Convert.ToBase64String(key);
        }

        /// <summary>
        /// Verify a password against the stored Base64 salt and Base64 hash.
        /// Returns true if the password matches.
        /// </summary>
        public static bool Verify(string password, string saltBase64, string hashBase64)
        {
            if (password == null) throw new ArgumentNullException(nameof(password));
            if (saltBase64 == null) throw new ArgumentNullException(nameof(saltBase64));
            if (hashBase64 == null) throw new ArgumentNullException(nameof(hashBase64));

            var computed = HashPassword(password, saltBase64);
            var computedBytes = Convert.FromBase64String(computed);
            var storedBytes = Convert.FromBase64String(hashBase64);

            // Constant-time comparison to avoid timing attacks
            return CryptographicOperations.FixedTimeEquals(computedBytes, storedBytes);
        }

        /// <summary>
        /// Convenience method: create salt and hash for a plain password.
        /// Returns a tuple (saltBase64, hashBase64).
        /// </summary>
        public static (string SaltBase64, string HashBase64) CreateSaltAndHash(string password)
        {
            var salt = CreateSalt();
            var hash = HashPassword(password, salt);
            return (salt, hash);
        }


    }
}
