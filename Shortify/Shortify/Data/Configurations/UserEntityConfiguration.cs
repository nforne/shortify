// File: Data/Configurations/UserEntityConfiguration.cs
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Shortify.Models;
using static Shortify.Data.Configurations.Pbkdf2Seeder;


namespace Shortify.Data.Configurations
{
    public class UserEntityConfiguration : IEntityTypeConfiguration<UserEntity>
    {

        public void Configure(EntityTypeBuilder<UserEntity> builder)
        {
            // Configure table and check constraint using the ToTable overload
            builder.ToTable("Users", t =>
            {
                t.HasCheckConstraint(
                    "CK_Users_HasName",
                    "(([DisplayName] IS NOT NULL AND LTRIM(RTRIM([DisplayName])) <> '') OR ([FirstName] IS NOT NULL AND LTRIM(RTRIM([FirstName])) <> '') OR ([LastName] IS NOT NULL AND LTRIM(RTRIM([LastName])) <> ''))"
                );
            });

            builder.HasKey(x => x.Id);
            builder.Property(x => x.Id).ValueGeneratedOnAdd();

            builder.Property(x => x.TenantId).IsRequired().HasMaxLength(200);

            // Email is globally unique across the system
            builder.Property(x => x.Email).IsRequired().HasMaxLength(320);
            builder.HasIndex(x => x.Email).IsUnique();

            // Names and display name
            builder.Property(x => x.FirstName).HasMaxLength(100).IsRequired(false);
            builder.Property(x => x.LastName).HasMaxLength(100).IsRequired(false);
            builder.Property(x => x.DisplayName).HasMaxLength(200).IsRequired(false);

            builder.Property(x => x.RolesJson).HasColumnType("nvarchar(max)");
            builder.Property(x => x.Status).HasMaxLength(50);

            // Authentication fields
            builder.Property(x => x.PasswordHash)
                .HasColumnType("nvarchar(max)")
                .IsRequired(false);

            builder.Property(x => x.PasswordSalt)
                .HasMaxLength(256)
                .HasColumnType("nvarchar(256)")
                .IsRequired(false);

            builder.Property(x => x.PasswordChangedAt)
                .HasColumnType("datetime2")
                .IsRequired(false);

            builder.Property(x => x.CreatedAt)
                .HasColumnType("datetime2")
                .HasDefaultValueSql("SYSUTCDATETIME()");

            // Test password to seed
            var testPassword = "tenant_abc123";

            // Deterministic salts for repeatable migrations
            var adminSalt = CreateSaltFromString("seed:tenant_abc123:admin");
            var aliceSalt = CreateSaltFromString("seed:tenant_abc123:alice");
            var bobSalt = CreateSaltFromString("seed:tenant_abc123:bob");

            var adminHash = HashPassword(testPassword, adminSalt);
            var aliceHash = HashPassword(testPassword, aliceSalt);
            var bobHash = HashPassword(testPassword, bobSalt);

            builder.HasData(
                new UserEntity
                {
                    Id = 1,
                    TenantId = "tenant_abc123",
                    Email = "admin@shortify.local",
                    FirstName = "Admin",
                    LastName = null,
                    // DisplayName intentionally left null to exercise fallback to FirstName
                    RolesJson = JsonSerializer.Serialize(new[] { "account-root" }),
                    Status = "active",
                    CreatedAt = DateTime.UtcNow,
                    PasswordSalt = adminSalt,
                    PasswordHash = adminHash,
                    PasswordChangedAt = DateTime.UtcNow
                },
                new UserEntity
                {
                    Id = 2,
                    TenantId = "tenant_abc123",
                    Email = "alice@shortify.local",
                    FirstName = "Alice",
                    LastName = "Anderson",
                    // DisplayName left null to fall back to FirstName
                    RolesJson = JsonSerializer.Serialize(new[] { "user" }),
                    Status = "active",
                    CreatedAt = DateTime.UtcNow,
                    PasswordSalt = aliceSalt,
                    PasswordHash = aliceHash,
                    PasswordChangedAt = DateTime.UtcNow
                },
                new UserEntity
                {
                    Id = 3,
                    TenantId = "tenant_abc123",
                    Email = "bob@shortify.local",
                    FirstName = "Bob",
                    LastName = "Baker",
                    RolesJson = JsonSerializer.Serialize(new[] { "user" }),
                    Status = "active",
                    CreatedAt = DateTime.UtcNow,
                    PasswordSalt = bobSalt,
                    PasswordHash = bobHash,
                    PasswordChangedAt = DateTime.UtcNow
                }
            );
        }

        /*
        
        //SignIn
        {
          "email": "admin@shortify.local",
          "password": "tenant_abc123"
        }

        {
        "email": "john.doe+a@example.com",
        "password": "Str0ngP@ssw0rd!"
        }

        //SignUpDto

        {
          "TenantName": "shortify-tenant-001",
          "AdminEmail": "jane.doe@example.com",
          "AdminPassword": "Str0ngP@ssw0rd!",
          "DisplayName": "Jane Doe",
          "FirstName": "Jane",
          "LastName": "Doe"
        }

        //CreateUserDto
        {
          "TenantId": "shortify-tenant-001",
          "Email": "jane.doe@example.com",
          "FirstName": "Jane",
          "LastName": "Doe",
          "DisplayName": "Jane Doe",
          "Roles": ["User"],
          "Password": "P@ssw0rd!2025"
        }



        */

        //// Deterministic salt generator for seeding
        //private static string CreateSaltFromString(string seed)
        //{
        //    using var sha = SHA256.Create();
        //    var bytes = sha.ComputeHash(Encoding.UTF8.GetBytes(seed));
        //    var saltBytes = bytes.Take(SaltSize).ToArray();
        //    return Convert.ToBase64String(saltBytes);
        //}

        //// PBKDF2 hash -> base64
        //private static string HashPassword(string password, string saltBase64)
        //{
        //    var salt = Convert.FromBase64String(saltBase64);
        //    using var derive = new Rfc2898DeriveBytes(password, salt, Iterations, HashAlgorithmName.SHA256);
        //    var key = derive.GetBytes(KeySize);
        //    return Convert.ToBase64String(key);
        //}

        //// Requires: using System;
        //// using System.Security.Cryptography;
        //public static bool VerifyPassword(string password, string saltBase64, string expectedHashBase64)
        //{
        //    if (password is null) throw new ArgumentNullException(nameof(password));
        //    if (saltBase64 is null) throw new ArgumentNullException(nameof(saltBase64));
        //    if (expectedHashBase64 is null) throw new ArgumentNullException(nameof(expectedHashBase64));

        //    var salt = Convert.FromBase64String(saltBase64);
        //    var expected = Convert.FromBase64String(expectedHashBase64);

        //    using var derive = new Rfc2898DeriveBytes(password, salt, Iterations, HashAlgorithmName.SHA256);
        //    var key = derive.GetBytes(KeySize);

        //    // Constant-time comparison to avoid timing attacks
        //    return CryptographicOperations.FixedTimeEquals(key, expected);
        //}

    }

    public static class Pbkdf2Seeder
    {
        // PBKDF2 parameters used for seeding
        private const int SaltSize = 16;    // bytes
        private const int KeySize = 32;     // bytes
        private const int Iterations = 100_000;

        // Deterministic salt generator for seeding
        public static string CreateSaltFromString(string seed)
        {
            if (seed is null) throw new ArgumentNullException(nameof(seed));
            using var sha = SHA256.Create();
            var bytes = sha.ComputeHash(Encoding.UTF8.GetBytes(seed));
            var saltBytes = bytes.Take(SaltSize).ToArray();
            return Convert.ToBase64String(saltBytes);
        }

        // PBKDF2 hash -> base64
        public static string HashPassword(string password, string saltBase64)
        {
            if (password is null) throw new ArgumentNullException(nameof(password));
            if (saltBase64 is null) throw new ArgumentNullException(nameof(saltBase64));

            var salt = Convert.FromBase64String(saltBase64);
            using var derive = new Rfc2898DeriveBytes(password, salt, Iterations, HashAlgorithmName.SHA256);
            var key = derive.GetBytes(KeySize);
            return Convert.ToBase64String(key);
        }

        /// <summary>
        /// Verify a password against a stored Base64 hash using the provided Base64 salt.
        /// Returns true only if the derived key matches the stored key (constant-time compare).
        /// Returns false for malformed inputs (invalid Base64) or length mismatches.
        /// </summary>
        public static bool VerifyPassword(string password, string saltBase64, string hashBase64)
        {
            if (password is null) throw new ArgumentNullException(nameof(password));
            if (saltBase64 is null) throw new ArgumentNullException(nameof(saltBase64));
            if (hashBase64 is null) throw new ArgumentNullException(nameof(hashBase64));

            byte[] salt;
            byte[] stored;
            try
            {
                salt = Convert.FromBase64String(saltBase64);
                stored = Convert.FromBase64String(hashBase64);
            }
            catch (FormatException)
            {
                // Malformed Base64 input — treat as verification failure
                return false;
            }

            using var derive = new Rfc2898DeriveBytes(password, salt, Iterations, HashAlgorithmName.SHA256);
            var computed = derive.GetBytes(KeySize);

            if (computed.Length != stored.Length) return false;

            return CryptographicOperations.FixedTimeEquals(computed, stored);
        }

        /// <summary>
        /// Convenience: verify using the deterministic seed string used to derive the salt.
        /// Example seed: "seed:tenant_abc123:admin"
        /// </summary>
        public static bool VerifyWithSeed(string password, string seed, string hashBase64)
        {
            if (seed is null) throw new ArgumentNullException(nameof(seed));
            var saltBase64 = CreateSaltFromString(seed);
            return VerifyPassword(password, saltBase64, hashBase64);
        }
    }

}


