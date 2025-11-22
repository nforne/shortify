using Shortify.Models;
using Microsoft.EntityFrameworkCore;
using Shortify.Data.Configurations;

namespace Shortify.Data
{
    public class ShortifyDbContext : DbContext
    {
        public ShortifyDbContext(DbContextOptions<ShortifyDbContext> opts) : base(opts) { }

        public DbSet<UserEntity> Users { get; set; } = null!;

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            //modelBuilder.Entity<UserEntity>(b =>
            //{
            //    b.HasKey(u => u.Id);
            //    b.Property(u => u.TenantId).IsRequired().HasMaxLength(200);
            //    b.Property(u => u.Email).IsRequired().HasMaxLength(320);
            //    b.HasIndex(u => new { u.TenantId, u.Email }).IsUnique();
            //    b.Property(u => u.RolesJson).HasColumnType("nvarchar(max)");
            //    b.Property(u => u.Status).HasMaxLength(50);
            //});

            modelBuilder.ApplyConfiguration(new UserEntityConfiguration());
        }
    }
}
