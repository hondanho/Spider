using DotnetCrawler.Data.Models.Novel;
using Microsoft.EntityFrameworkCore;
using System;
using System.Threading.Tasks;
using System.Threading;

namespace DotnetCrawler.Data.Models
{
    public partial class CatalogDbContext : DbContext
    {
        public CatalogDbContext()
        {
        }

        public CatalogDbContext(DbContextOptions<CatalogDbContext> options)
            : base(options)
        {
        }

        public virtual DbSet<PostDb> PostDb { get; set; }
        public virtual DbSet<ChapDb> ChapDb { get; set; }
        public virtual DbSet<CategoryDb> CategoryDb { get; set; }
        public virtual DbSet<SettingDb> SettingDb { get; set; }


        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            if (!optionsBuilder.IsConfigured)
            {
                optionsBuilder.UseSqlServer("Server=DESKTOP-NT20HK6\\MSSQLSERVER01;Database=WP;Trusted_Connection=True;");
            }
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
        }

        public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default(CancellationToken))
        {
            UpdateTimestamps();
            return await base.SaveChangesAsync(cancellationToken);
        }

        public override int SaveChanges()
        {
            UpdateTimestamps();
            return base.SaveChanges();
        }

        private void UpdateTimestamps()
        {
            foreach (var entry in ChangeTracker.Entries())
            {
                if (entry.Entity is BaseEntity entity)
                {
                    var now = DateTime.UtcNow;

                    if (entry.State == EntityState.Added)
                    {
                        entity.CreateDate = now;
                        entity.UpdateDate = now;
                    }
                    else if (entry.State == EntityState.Modified)
                    {
                        entity.UpdateDate = now;
                    }
                }
            }
        }
    }
}
