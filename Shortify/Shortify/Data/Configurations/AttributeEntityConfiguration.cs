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
            builder.HasKey(a => a.Id);
            builder.Property(a => a.TenantId).IsRequired();
            builder.Property(a => a.Key).IsRequired().HasMaxLength(200);
            builder.Property(a => a.ValueType).IsRequired().HasMaxLength(50);
            builder.Property(a => a.Visibility).IsRequired().HasMaxLength(50);
            builder.Property(a => a.OptionsJson).HasColumnType("nvarchar(max)");
            builder.Property(a => a.Status).IsRequired().HasMaxLength(50);
            builder.HasIndex(a => new { a.TenantId, a.Key }).IsUnique(false);
            
            builder.HasData(
               new AttributeEntity
               {
                   Id = 1,
                   TenantId = "tenant_abc123",
                   Key = "campaign_source",
                   ValueType = "string",
                   Visibility = "public",
                   OptionsJson = null,
                   Status = "active",
                   CreatedAt = DateTime.UtcNow
               },
               new AttributeEntity
               {
                   Id = 2,
                   TenantId = "tenant_abc123",
                   Key = "campaign_source",
                   ValueType = "string",
                   Visibility = "public",
                   OptionsJson = null,
                   Status = "active",
                   CreatedAt = DateTime.UtcNow
               }


            );
        }
    }
}
