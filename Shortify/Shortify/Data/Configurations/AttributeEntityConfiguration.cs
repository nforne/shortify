using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Shortify.Models;

namespace Shortify.Data.Configurations
{
    public class AttributeEntityConfiguration : IEntityTypeConfiguration<AttributeEntity>
    {
        public void Configure(EntityTypeBuilder<AttributeEntity> builder)
        {
            builder.ToTable("Attributes");

            // PK: ensure database generates the Id by default
            builder.HasKey(a => a.Id);
            builder.Property(a => a.Id).ValueGeneratedOnAdd();

            builder.Property(a => a.TenantId).IsRequired();
            builder.Property(a => a.Key).IsRequired().HasMaxLength(200);
            builder.Property(a => a.ValueType).IsRequired().HasMaxLength(50);
            builder.Property(a => a.Visibility).IsRequired().HasMaxLength(50);
            builder.Property(a => a.OptionsJson).HasColumnType("nvarchar(max)");
            builder.Property(a => a.Status).IsRequired().HasMaxLength(50);

            // Resolve / shortlink related columns
            builder.Property(a => a.AccountRootUserPk).IsRequired().HasDefaultValue(0);
            builder.Property(a => a.ResolveUrl).HasMaxLength(2000).IsRequired(false);
            builder.Property(a => a.ResolveSignedByDefault).IsRequired().HasDefaultValue(false);
            builder.Property(a => a.ResolveCacheTtlSeconds).IsRequired(false);
            builder.Property(a => a.RedirectOnResolve).IsRequired().HasDefaultValue(false);

            builder.HasIndex(a => new { a.TenantId, a.Key }).IsUnique(false);
            builder.HasIndex(a => new { a.AccountRootUserPk, a.Id }).HasDatabaseName("IX_Attributes_AccountRoot_AttrId");

            // Seed data (five sample attributes). Id values are included for deterministic seeds,
            // but Id remains DB-generated for new rows because ValueGeneratedOnAdd() is set.
            builder.HasData(
               new AttributeEntity
               {
                   Id = 100, // seed id for deterministic test data; DB will still auto-generate for new inserts
                   TenantId = "tenant_abc123",
                   Key = "campaign_source_google",
                   ValueType = "string",
                   Visibility = "public",
                   OptionsJson = null,
                   Status = "active",
                   CreatedAt = DateTime.UtcNow.AddMinutes(-30),
                   AccountRootUserPk = 10,
                   ResolveUrl = "https://www.google.com",
                   ResolveSignedByDefault = false,
                   ResolveCacheTtlSeconds = 30,
                   RedirectOnResolve = false
               },
               new AttributeEntity
               {
                   Id = 101,
                   TenantId = "tenant_abc123",
                   Key = "campaign_source_example",
                   ValueType = "string",
                   Visibility = "public",
                   OptionsJson = null,
                   Status = "active",
                   CreatedAt = DateTime.UtcNow.AddMinutes(-25),
                   AccountRootUserPk = 10,
                   ResolveUrl = "https://example.com",
                   ResolveSignedByDefault = false,
                   ResolveCacheTtlSeconds = 30,
                   RedirectOnResolve = false
               },
               new AttributeEntity
               {
                   Id = 102,
                   TenantId = "tenant_abc123",
                   Key = "campaign_source_httpbin",
                   ValueType = "string",
                   Visibility = "public",
                   OptionsJson = null,
                   Status = "active",
                   CreatedAt = DateTime.UtcNow.AddMinutes(-20),
                   AccountRootUserPk = 20,
                   ResolveUrl = "https://httpbin.org/json",
                   ResolveSignedByDefault = false,
                   ResolveCacheTtlSeconds = 30,
                   RedirectOnResolve = false
               },
               new AttributeEntity
               {
                   Id = 103,
                   TenantId = "tenant_abc123",
                   Key = "campaign_source_github",
                   ValueType = "string",
                   Visibility = "public",
                   OptionsJson = null,
                   Status = "active",
                   CreatedAt = DateTime.UtcNow.AddMinutes(-15),
                   AccountRootUserPk = 20,
                   ResolveUrl = "https://github.com",
                   ResolveSignedByDefault = false,
                   ResolveCacheTtlSeconds = 30,
                   RedirectOnResolve = false
               },
               new AttributeEntity
               {
                   Id = 104,
                   TenantId = "tenant_abc123",
                   Key = "campaign_source_stackoverflow",
                   ValueType = "string",
                   Visibility = "public",
                   OptionsJson = null,
                   Status = "active",
                   CreatedAt = DateTime.UtcNow.AddMinutes(-10),
                   AccountRootUserPk = 30,
                   ResolveUrl = "https://stackoverflow.com",
                   ResolveSignedByDefault = false,
                   ResolveCacheTtlSeconds = 30,
                   RedirectOnResolve = false
               }
            );
        }
    }
}
