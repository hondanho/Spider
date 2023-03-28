using System;
using DotnetCrawler.Data.Models.Novel;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;

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
#warning To protect potentially sensitive information in your connection string, you should move it out of source code. See http://go.microsoft.com/fwlink/?LinkId=723263 for guidance on storing connection strings.
                optionsBuilder.UseSqlServer("Server=localhost\\MSSQLSERVER01;Database=WP;Trusted_Connection=True;");
            }
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
        }
    }
}
