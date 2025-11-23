using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Shortify.Models;

namespace Shortify.Data.Configurations
{
    public class UserEntityConfiguration : IEntityTypeConfiguration<UserEntity>
    {
        public void Configure(EntityTypeBuilder<UserEntity> builder)
        {
            builder.ToTable("Users");
            builder.HasKey(x => x.Id);
            builder.Property(x => x.TenantId).IsRequired().HasMaxLength(200);
            builder.Property(x => x.Email).IsRequired().HasMaxLength(320);
            builder.HasIndex(x => new { x.TenantId, x.Email }).IsUnique();
            builder.Property(x => x.RolesJson).HasColumnType("nvarchar(max)");
            builder.Property(x => x.Status).HasMaxLength(50);

            builder.HasData(
                new UserEntity
                {
                    Id = 1,
                    TenantId = "tenant_abc123",
                    Email = "admin@shortify.local",
                    DisplayName = "Admin",
                    RolesJson = JsonSerializer.Serialize(new[] { "account-root" }),
                    Status = "active",
                    CreatedAt = DateTime.UtcNow
                },
                new UserEntity
                {
                    Id = 2,
                    TenantId = "tenant_abc123",
                    Email = "alice@shortify.local",
                    DisplayName = "Alice",
                    RolesJson = JsonSerializer.Serialize(new[] { "user" }),
                    Status = "active",
                    CreatedAt = DateTime.UtcNow
                },
                new UserEntity
                {
                    Id = 3,
                    TenantId = "tenant_abc123",
                    Email = "bob@shortify.local",
                    DisplayName = "Bob",
                    RolesJson = JsonSerializer.Serialize(new[] { "user" }),
                    Status = "active",
                    CreatedAt = DateTime.UtcNow
                }
            );
        }
    }
}
