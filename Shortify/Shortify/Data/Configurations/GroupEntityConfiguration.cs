using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Shortify.Models;

namespace Shortify.Data.Configurations
{
    public class GroupEntityConfiguration : IEntityTypeConfiguration<GroupEntity>
    {
        public void Configure(EntityTypeBuilder<GroupEntity> builder)
        {
            builder.ToTable("Groups");
            builder.HasKey(g => g.Id);
            builder.Property(g => g.TenantId).IsRequired();
            builder.Property(g => g.Name).IsRequired();
            builder.Property(g => g.Type).IsRequired();
            builder.Property(g => g.MetadataJson).HasColumnType("nvarchar(max)");
            builder.Property(g => g.Status).HasMaxLength(32).HasDefaultValue("active");
            builder.HasIndex(g => new { g.TenantId, g.Name }).IsUnique();

            builder.HasData(
                new GroupEntity { Id = 1, TenantId = "tenant_abc123", Name = "group_marketing", Type = "user", MetadataJson = "{}", Status = "active", CreatedAt = DateTime.UtcNow.AddDays(-1) },
                new GroupEntity { Id = 2, TenantId = "tenant_abc123", Name = "group_ops", Type = "user", MetadataJson = "{}", Status = "active", CreatedAt = DateTime.UtcNow.AddDays(-1) }
            );
        }
    }
}
