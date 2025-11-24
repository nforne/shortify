using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Shortify.Models;

namespace Shortify.Data.Configurations
{
    public class ResolveEventConfiguration : IEntityTypeConfiguration<ResolveEventEntity>
    {
        public void Configure(EntityTypeBuilder<ResolveEventEntity> builder)
        {
            builder.ToTable("ResolveEvents");

            // PK generation
            builder.HasKey(e => e.Id);
            builder.Property(e => e.Id).ValueGeneratedOnAdd();

            builder.Property(e => e.ResolveUrl).HasMaxLength(2000);
            builder.Property(e => e.CallerIp).HasMaxLength(100);
            builder.Property(e => e.UserAgent).HasMaxLength(1000);
            builder.Property(e => e.Outcome).IsRequired().HasMaxLength(50);
            builder.Property(e => e.ErrorMessage).HasMaxLength(2000);

            builder.HasIndex(e => new { e.AccountRootUserPk, e.AttributeId }).HasDatabaseName("IX_ResolveEvents_AccountRoot_Attr");
            builder.HasIndex(e => e.CreatedAt);

            // Seed data (deterministic PKs for test/dev). IDs here are explicit but runtime inserts will be DB-generated.
            // These seeds reference attribute Ids 100..104 seeded in Attribute configuration.
            builder.HasData(
                new ResolveEventEntity
                {
                    Id = 1000,
                    AttributeId = 100,
                    AccountRootUserPk = 10,
                    ResolveUrl = "https://www.google.com",
                    CallerIp = "127.0.0.1",
                    UserAgent = "Seeder/1.0",
                    Outcome = "success",
                    CacheHit = false,
                    ErrorMessage = null,
                    CreatedAt = DateTime.UtcNow.AddMinutes(-9)
                },
                new ResolveEventEntity
                {
                    Id = 1001,
                    AttributeId = 101,
                    AccountRootUserPk = 10,
                    ResolveUrl = "https://example.com",
                    CallerIp = "127.0.0.1",
                    UserAgent = "Seeder/1.0",
                    Outcome = "success",
                    CacheHit = true,
                    ErrorMessage = null,
                    CreatedAt = DateTime.UtcNow.AddMinutes(-8)
                },
                new ResolveEventEntity
                {
                    Id = 1002,
                    AttributeId = 102,
                    AccountRootUserPk = 20,
                    ResolveUrl = "https://httpbin.org/json",
                    CallerIp = "127.0.0.1",
                    UserAgent = "Seeder/1.0",
                    Outcome = "failed",
                    CacheHit = false,
                    ErrorMessage = "Upstream timeout",
                    CreatedAt = DateTime.UtcNow.AddMinutes(-7)
                },
                new ResolveEventEntity
                {
                    Id = 1003,
                    AttributeId = 103,
                    AccountRootUserPk = 20,
                    ResolveUrl = "https://github.com",
                    CallerIp = "127.0.0.1",
                    UserAgent = "Seeder/1.0",
                    Outcome = "success",
                    CacheHit = false,
                    ErrorMessage = null,
                    CreatedAt = DateTime.UtcNow.AddMinutes(-6)
                },
                new ResolveEventEntity
                {
                    Id = 1004,
                    AttributeId = 104,
                    AccountRootUserPk = 30,
                    ResolveUrl = "https://stackoverflow.com",
                    CallerIp = "127.0.0.1",
                    UserAgent = "Seeder/1.0",
                    Outcome = "not_found",
                    CacheHit = false,
                    ErrorMessage = "Attribute not found",
                    CreatedAt = DateTime.UtcNow.AddMinutes(-5)
                }
            );
        }
    }
}
