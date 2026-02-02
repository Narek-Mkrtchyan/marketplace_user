using Catalog.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace Catalog.Api.Data;

public class CatalogDbContext : DbContext
{
    public CatalogDbContext(DbContextOptions<CatalogDbContext> options) : base(options) {}

    public DbSet<Category> Categories => Set<Category>();

    public DbSet<Region> Regions => Set<Region>();
    public DbSet<RegionI18n> RegionI18n => Set<RegionI18n>();
    public DbSet<City> Cities => Set<City>();
    public DbSet<CityI18n> CityI18n => Set<CityI18n>();

    public DbSet<Listing> Listings => Set<Listing>();
    public DbSet<ListingPhoto> ListingPhotos => Set<ListingPhoto>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Category>(e =>
        {
            e.ToTable("categories", "public");
            e.HasKey(x => x.Id);

            e.Property(x => x.Id).HasColumnName("id");
            e.Property(x => x.Name).HasColumnName("name");
            e.Property(x => x.Slug).HasColumnName("slug");
            e.Property(x => x.ParentId).HasColumnName("parent_id");
            e.Property(x => x.IsEnabled).HasColumnName("is_enabled");
            e.Property(x => x.CreatedAtUtc).HasColumnName("created_at_utc");
            e.Property(x => x.UpdatedAtUtc).HasColumnName("updated_at_utc");
        });

        // ===== Regions =====
        modelBuilder.Entity<Region>(e =>
        {
            e.ToTable("regions", "public");
            e.HasKey(x => x.Id);

            e.Property(x => x.Id).HasColumnName("id");
            e.Property(x => x.Code).HasColumnName("code");
            e.Property(x => x.IsActive).HasColumnName("is_active");

            e.HasMany(x => x.I18n).WithOne(x => x.Region).HasForeignKey(x => x.RegionId);
            e.HasMany(x => x.Cities).WithOne(x => x.Region).HasForeignKey(x => x.RegionId);
        });

        modelBuilder.Entity<RegionI18n>(e =>
        {
            e.ToTable("region_i18n", "public");
            e.HasKey(x => new { x.RegionId, x.Lang });

            e.Property(x => x.RegionId).HasColumnName("region_id");
            e.Property(x => x.Lang).HasColumnName("lang");
            e.Property(x => x.Name).HasColumnName("name");
        });

        // ===== Cities =====
        modelBuilder.Entity<City>(e =>
        {
            e.ToTable("cities", "public");
            e.HasKey(x => x.Id);

            e.Property(x => x.Id).HasColumnName("id");
            e.Property(x => x.RegionId).HasColumnName("region_id");
            e.Property(x => x.Code).HasColumnName("code");
            e.Property(x => x.IsActive).HasColumnName("is_active");

            e.HasMany(x => x.I18n).WithOne(x => x.City).HasForeignKey(x => x.CityId);
        });

        modelBuilder.Entity<CityI18n>(e =>
        {
            e.ToTable("city_i18n", "public");
            e.HasKey(x => new { x.CityId, x.Lang });

            e.Property(x => x.CityId).HasColumnName("city_id");
            e.Property(x => x.Lang).HasColumnName("lang");
            e.Property(x => x.Name).HasColumnName("name");
        });

        // ===== Listings =====
        modelBuilder.Entity<Listing>(e =>
        {
            e.ToTable("listings", "public");
            e.HasKey(x => x.Id);

            e.Property(x => x.Id).HasColumnName("id");
            e.Property(x => x.OwnerUserId).HasColumnName("owner_user_id");
            e.Property(x => x.CategoryId).HasColumnName("category_id");
            e.Property(x => x.CityId).HasColumnName("city_id");
            e.Property(x => x.IsPublished).HasColumnName("is_published");

            e.Property(x => x.Title).HasColumnName("title");
            e.Property(x => x.Description).HasColumnName("description"); 
            e.Property(x => x.Price).HasColumnName("price");

            e.Property(x => x.CreatedAtUtc).HasColumnName("created_at_utc");
            e.Property(x => x.UpdatedAtUtc).HasColumnName("updated_at_utc");

            e.HasOne(x => x.Category)
                .WithMany()
                .HasForeignKey(x => x.CategoryId)
                .OnDelete(DeleteBehavior.Restrict);
            
            e.HasOne(x => x.City)
                .WithMany()
                .HasForeignKey(x => x.CityId);

            e.HasMany(x => x.Photos)
                .WithOne(p => p.Listing)
                .HasForeignKey(p => p.ListingId)
                .OnDelete(DeleteBehavior.Cascade); 

        });


        modelBuilder.Entity<ListingPhoto>(e =>
        {
            e.ToTable("listing_photos", "public");
            e.HasKey(x => x.Id);

            e.Property(x => x.Id).HasColumnName("id");
            e.Property(x => x.ListingId).HasColumnName("listing_id");
            e.Property(x => x.Url).HasColumnName("url");
            e.Property(x => x.SortOrder).HasColumnName("sort_order");
            e.Property(x => x.IsMain).HasColumnName("is_main");
            e.Property(x => x.CreatedAtUtc).HasColumnName("created_at_utc");
        });
    }
}
