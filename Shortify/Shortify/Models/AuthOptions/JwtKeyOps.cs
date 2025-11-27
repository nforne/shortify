using System.Security.Cryptography;
using Microsoft.IdentityModel.Tokens;

namespace Shortify.Models.AuthOptions
{
    public static class JwtKeyOps
    {
        // Generate cryptographically secure random bytes (32 bytes = 256 bits)
        public static byte[] GenerateRandomKeyBytes(int sizeInBytes = 32)
        {
            var bytes = new byte[sizeInBytes];
            using var rng = RandomNumberGenerator.Create();
            rng.GetBytes(bytes);
            return bytes;
        }

        // Return a lowercase hex string for storage or display
        public static string ToHex(byte[] bytes) =>
            Convert.ToHexString(bytes).ToLowerInvariant();

        // Convert hex string back to bytes (throws on invalid input)
        public static byte[] FromHex(string hex)
        {
            if (string.IsNullOrWhiteSpace(hex))
                throw new ArgumentException("Hex string is null or empty.", nameof(hex));
            // .NET 5+ has Convert.FromHexString
            #if NET5_0_OR_GREATER
                        return Convert.FromHexString(hex);
            #else
                    if (hex.Length % 2 != 0) throw new ArgumentException("Hex string must have even length.", nameof(hex));
                    var bytes = new byte[hex.Length / 2];
                    for (int i = 0; i < bytes.Length; i++)
                        bytes[i] = Convert.ToByte(hex.Substring(i * 2, 2), 16);
                    return bytes;
            #endif
        }

        // Base64 encode bytes for config storage
        public static string ToBase64(byte[] bytes) =>
            Convert.ToBase64String(bytes);

        // Decode base64 back to bytes
        public static byte[] FromBase64(string base64)
        {
            if (string.IsNullOrWhiteSpace(base64))
                throw new ArgumentException("Base64 string is null or empty.", nameof(base64));
            return Convert.FromBase64String(base64);
        }

        // Validate key length (HS256 requires >= 32 bytes)
        public static void ValidateMinimumKeyLength(byte[] keyBytes, int minBytes = 32)
        {
            if (keyBytes == null) throw new ArgumentNullException(nameof(keyBytes));
            if (keyBytes.Length < minBytes)
                throw new InvalidOperationException($"Key too short: {keyBytes.Length} bytes. Must be at least {minBytes} bytes.");
        }

        // Create SymmetricSecurityKey and SigningCredentials for HS256
        public static SigningCredentials CreateSigningCredentialsFromBytes(byte[] keyBytes)
        {
            ValidateMinimumKeyLength(keyBytes);
            var signingKey = new SymmetricSecurityKey(keyBytes);
            return new SigningCredentials(signingKey, SecurityAlgorithms.HmacSha256);
        }

        // Convenience: create SigningCredentials from Base64 config value
        public static SigningCredentials CreateSigningCredentialsFromBase64(string base64)
        {
            var bytes = FromBase64(base64);
            return CreateSigningCredentialsFromBytes(bytes);
        }

        // Convenience: create SigningCredentials from hex config value
        public static SigningCredentials CreateSigningCredentialsFromHex(string hex)
        {
            var bytes = FromHex(hex);
            return CreateSigningCredentialsFromBytes(bytes);
        }

        // Helper to produce a secure key and return both hex and base64 representations
        public static (byte[] Bytes, string Hex, string Base64) GenerateKeyAndFormats(int sizeInBytes = 32)
        {
            var bytes = GenerateRandomKeyBytes(sizeInBytes);
            return (bytes, ToHex(bytes), ToBase64(bytes));
        }
    }
}