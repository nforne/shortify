using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Newtonsoft.Json;
using Shortify.Models;

namespace Shortify.Data.Configurations
{
    public class MetricEntityConfiguration : IEntityTypeConfiguration<MetricEntity>
    {
        public void Configure(EntityTypeBuilder<MetricEntity> builder)
        {
            builder.ToTable("Metrics");
            builder.HasKey(m => m.Id);
            builder.Property(m => m.Id).ValueGeneratedOnAdd();

            builder.Property(m => m.TenantId).IsRequired();
            builder.Property(m => m.Name).IsRequired();
            builder.Property(m => m.SummaryJson).HasColumnType("nvarchar(max)");
            builder.Property(m => m.StorageUrl).HasMaxLength(2000);
            builder.Property(m => m.StorageKey).HasMaxLength(1000);


            builder.Property(m => m.FileFormats).HasConversion(
                v => string.Join(",", v),
                v => v.Split(',', StringSplitOptions.RemoveEmptyEntries)
            ).HasColumnName("FileFormats");
            builder.Property(m => m.RowVersion).IsRowVersion();


            builder.HasIndex(m => new { m.TenantId, m.Name, m.From, m.To }).IsUnique(false);

            builder.HasData(

                new MetricEntity
                {
                    Id = 1,
                    TenantId = "tenant_abc123",
                    Name = "metric_campaign_source",
                    From = DateTime.UtcNow.AddDays(-30),
                    To = DateTime.UtcNow,
                    Status = "ready",
                    StorageKey = null,
                    StorageUrl = null,
                    FileFormats = new[] { "csv" },
                    CreatedBy = "system",
                    CreatedAt = DateTime.UtcNow
                },
                new MetricEntity
                {
                    Id = 2,
                    TenantId = "tenant_abc123",
                    Name = "test_attr_resolution_source",
                    From = DateTime.UtcNow.AddDays(-30),
                    To = DateTime.UtcNow,
                    Status = "ready",
                    StorageKey = null,
                    StorageUrl = null,
                    FileFormats = new[] { "json" },
                    CreatedBy = "system",
                    CreatedAt = DateTime.UtcNow
                }


            );
        }
    }
}
