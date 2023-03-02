using System.Reflection;
using EntityFrameworkCore.Triggers;
using Microsoft.EntityFrameworkCore;
using VirtoCommerce.SitemapsModule.Data.Models;

namespace VirtoCommerce.SitemapsModule.Data.Repositories
{
    public class SitemapDbContext : DbContextWithTriggers
    {
        public SitemapDbContext(DbContextOptions<SitemapDbContext> options)
            : base(options)
        {
        }

        protected SitemapDbContext(DbContextOptions options)
            : base(options)
        {
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<SitemapEntity>().ToTable("Sitemap").HasKey(x => x.Id);
            modelBuilder.Entity<SitemapEntity>().Property(x => x.Id).HasMaxLength(128).ValueGeneratedOnAdd();
            modelBuilder.Entity<SitemapEntity>().Property(x => x.Filename);
            modelBuilder.Entity<SitemapEntity>().HasIndex(x => x.Filename);

            modelBuilder.Entity<SitemapItemEntity>().ToTable("SitemapItem").HasKey(x => x.Id);
            modelBuilder.Entity<SitemapItemEntity>().Property(x => x.Id).HasMaxLength(128).ValueGeneratedOnAdd();
            modelBuilder.Entity<SitemapItemEntity>().HasOne(x => x.Sitemap).WithMany(x => x.Items).IsRequired()
                .HasForeignKey(x => x.SitemapId).OnDelete(DeleteBehavior.Cascade);

            base.OnModelCreating(modelBuilder);

            // Allows configuration for an entity type for different database types.
            // Applies configuration from all <see cref="IEntityTypeConfiguration{TEntity}" in VirtoCommerce.SitemapsModule.Data.XXX project. /> 
            switch (this.Database.ProviderName)
            {
                case "Pomelo.EntityFrameworkCore.MySql":
                    modelBuilder.ApplyConfigurationsFromAssembly(Assembly.Load("VirtoCommerce.SitemapsModule.Data.MySql"));
                    break;
                case "Npgsql.EntityFrameworkCore.PostgreSQL":
                    modelBuilder.ApplyConfigurationsFromAssembly(Assembly.Load("VirtoCommerce.SitemapsModule.Data.PostgreSql"));
                    break;
                case "Microsoft.EntityFrameworkCore.SqlServer":
                    modelBuilder.ApplyConfigurationsFromAssembly(Assembly.Load("VirtoCommerce.SitemapsModule.Data.SqlServer"));
                    break;
            }
        }
    }
}
