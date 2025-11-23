using Shortify.Models;
using Microsoft.EntityFrameworkCore;
using Shortify.Data.Configurations;

namespace Shortify.Data
{
    public class ShortifyDbContext : DbContext
    {
        public ShortifyDbContext(DbContextOptions<ShortifyDbContext> opts) : base(opts) { }

        public DbSet<UserEntity> Users { get; set; } = null!;

        // Add this line
        public DbSet<GroupEntity> Groups { get; set; } = null!;

        // other DbSets...
        public DbSet<AttributeEntity> Attributes { get; set; } = null!;

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            
            modelBuilder.ApplyConfiguration(new UserEntityConfiguration());
            modelBuilder.ApplyConfiguration(new GroupEntityConfiguration());
        }
    }
}
