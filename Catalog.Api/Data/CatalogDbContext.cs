using Catalog.Api.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace Catalog.Api.Data;

public class CatalogDbContext : DbContext
{
    public CatalogDbContext(DbContextOptions<CatalogDbContext> options) : base(options) {}

    public DbSet<Category> Categories => Set<Category>();
    public DbSet<CategoryTranslation> CategoryTranslations => Set<CategoryTranslation>();

    public DbSet<Region> Regions => Set<Region>();
    public DbSet<RegionI18n> RegionI18n => Set<RegionI18n>();

    public DbSet<City> Cities => Set<City>();
    public DbSet<CityI18n> CityI18n => Set<CityI18n>();

    public DbSet<CategoryAttribute> CategoryAttributes => Set<CategoryAttribute>();
    public DbSet<CategoryAttributeI18n> CategoryAttributeI18n => Set<CategoryAttributeI18n>();
    public DbSet<AttributeOption> AttributeOptions => Set<AttributeOption>();
    public DbSet<AttributeOptionI18n> AttributeOptionI18n => Set<AttributeOptionI18n>();

    public DbSet<Listing> Listings => Set<Listing>();
    public DbSet<ListingPhoto> ListingPhotos => Set<ListingPhoto>();
    public DbSet<ListingAttributeValue> ListingAttributeValues => Set<ListingAttributeValue>();

    private static readonly ValueConverter<AttributeValueType, string> AttributeTypeConverter =
        new(
            v => v == AttributeValueType.Select ? "select"
               : v == AttributeValueType.Number ? "number"
               : v == AttributeValueType.Bool   ? "bool"
               : "text",
            v => v == "select" ? AttributeValueType.Select
               : v == "number" ? AttributeValueType.Number
               : v == "bool"   ? AttributeValueType.Bool
               : AttributeValueType.Text
        );

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // ===== Categories =====
        modelBuilder.Entity<Category>(e =>
        {
            e.ToTable("categories", "public");
            e.HasKey(x => x.Id);

            e.Property(x => x.Id).HasColumnName("id");
            e.Property(x => x.Slug).HasColumnName("slug").IsRequired();
            e.Property(x => x.Icon).HasColumnName("icon");
            e.Property(x => x.SortOrder).HasColumnName("sort_order");

            e.Property(x => x.ParentId).HasColumnName("parent_id");
            e.Property(x => x.IsEnabled).HasColumnName("is_enabled");

            e.Property(x => x.CreatedAtUtc).HasColumnName("created_at_utc");
            e.Property(x => x.UpdatedAtUtc).HasColumnName("updated_at_utc");

            // parent -> children
            e.HasOne(x => x.Parent)
                .WithMany(x => x.Children)
                .HasForeignKey(x => x.ParentId)
                .OnDelete(DeleteBehavior.Cascade);

            // category -> translations
            e.HasMany(x => x.Translations)
                .WithOne(t => t.Category)
                .HasForeignKey(t => t.CategoryId)
                .OnDelete(DeleteBehavior.Cascade);

            // category -> attributes
            e.HasMany(x => x.Attributes)
                .WithOne(a => a.Category)
                .HasForeignKey(a => a.CategoryId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<CategoryTranslation>(e =>
        {
            e.ToTable("category_translations", "public");
            e.HasKey(x => x.Id);

            e.Property(x => x.Id).HasColumnName("id");
            e.Property(x => x.CategoryId).HasColumnName("category_id");
            e.Property(x => x.Lang).HasColumnName("lang").HasMaxLength(5).IsRequired();
            e.Property(x => x.Name).HasColumnName("name").IsRequired();

            e.HasIndex(x => new { x.CategoryId, x.Lang }).IsUnique();
        });

        // ===== Category Attributes =====
        modelBuilder.Entity<CategoryAttribute>(e =>
        {
            e.ToTable("category_attributes", "public");
            e.HasKey(x => x.Id);

            e.Property(x => x.Id).HasColumnName("id");
            e.Property(x => x.CategoryId).HasColumnName("category_id");

            e.Property(x => x.Code).HasColumnName("code").IsRequired();
            e.Property(x => x.Type).HasColumnName("type").HasConversion(AttributeTypeConverter);

            e.Property(x => x.IsRequired).HasColumnName("is_required");
            e.Property(x => x.SortOrder).HasColumnName("sort_order");
            e.Property(x => x.Unit).HasColumnName("unit");

            e.Property(x => x.CreatedAtUtc).HasColumnName("created_at_utc");
            e.Property(x => x.UpdatedAtUtc).HasColumnName("updated_at_utc");

            e.HasIndex(x => new { x.CategoryId, x.Code }).IsUnique();

            e.HasMany(x => x.I18n)
                .WithOne(x => x.Attribute)
                .HasForeignKey(x => x.AttributeId)
                .OnDelete(DeleteBehavior.Cascade);

            e.HasMany(x => x.Options)
                .WithOne(x => x.Attribute)
                .HasForeignKey(x => x.AttributeId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<CategoryAttributeI18n>(e =>
        {
            e.ToTable("category_attribute_i18n", "public");
            e.HasKey(x => new { x.AttributeId, x.Lang });

            e.Property(x => x.AttributeId).HasColumnName("attribute_id");
            e.Property(x => x.Lang).HasColumnName("lang").HasMaxLength(5).IsRequired();
            e.Property(x => x.Title).HasColumnName("title").IsRequired();
        });

        // ===== Attribute Options =====
        modelBuilder.Entity<AttributeOption>(e =>
        {
            e.ToTable("attribute_options", "public");
            e.HasKey(x => x.Id);

            e.Property(x => x.Id).HasColumnName("id");
            e.Property(x => x.AttributeId).HasColumnName("attribute_id");

            e.Property(x => x.Code).HasColumnName("code").IsRequired();
            e.Property(x => x.SortOrder).HasColumnName("sort_order");
            e.Property(x => x.IsActive).HasColumnName("is_active");

            e.HasIndex(x => new { x.AttributeId, x.Code }).IsUnique();

            e.HasMany(x => x.I18n)
                .WithOne(x => x.Option)
                .HasForeignKey(x => x.OptionId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<AttributeOptionI18n>(e =>
        {
            e.ToTable("attribute_option_i18n", "public");
            e.HasKey(x => new { x.OptionId, x.Lang });

            e.Property(x => x.OptionId).HasColumnName("option_id");
            e.Property(x => x.Lang).HasColumnName("lang").HasMaxLength(5).IsRequired();
            e.Property(x => x.Title).HasColumnName("title").IsRequired();

            e.HasOne(x => x.Option)
                .WithMany(o => o.I18n)
                .HasForeignKey(x => x.OptionId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // ===== Regions =====
        modelBuilder.Entity<Region>(e =>
        {
            e.ToTable("regions", "public");
            e.HasKey(x => x.Id);

            e.Property(x => x.Id).HasColumnName("id");
            e.Property(x => x.Code).HasColumnName("code").IsRequired();
            e.Property(x => x.IsActive).HasColumnName("is_active");

            e.HasMany(x => x.I18n)
                .WithOne(x => x.Region)
                .HasForeignKey(x => x.RegionId);

            e.HasMany(x => x.Cities)
                .WithOne(x => x.Region)
                .HasForeignKey(x => x.RegionId);
        });

        modelBuilder.Entity<RegionI18n>(e =>
        {
            e.ToTable("region_i18n", "public");
            e.HasKey(x => new { x.RegionId, x.Lang });

            e.Property(x => x.RegionId).HasColumnName("region_id");
            e.Property(x => x.Lang).HasColumnName("lang").HasMaxLength(5).IsRequired();
            e.Property(x => x.Name).HasColumnName("name").IsRequired();
        });

        // ===== Cities =====
        modelBuilder.Entity<City>(e =>
        {
            e.ToTable("cities", "public");
            e.HasKey(x => x.Id);

            e.Property(x => x.Id).HasColumnName("id");
            e.Property(x => x.RegionId).HasColumnName("region_id");
            e.Property(x => x.Code).HasColumnName("code").IsRequired();
            e.Property(x => x.IsActive).HasColumnName("is_active");

            e.HasMany(x => x.I18n)
                .WithOne(x => x.City)
                .HasForeignKey(x => x.CityId);
        });

        modelBuilder.Entity<CityI18n>(e =>
        {
            e.ToTable("city_i18n", "public");
            e.HasKey(x => new { x.CityId, x.Lang });

            e.Property(x => x.CityId).HasColumnName("city_id");
            e.Property(x => x.Lang).HasColumnName("lang").HasMaxLength(5).IsRequired();
            e.Property(x => x.Name).HasColumnName("name").IsRequired();
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

            e.Property(x => x.Title).HasColumnName("title").IsRequired();
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
            e.Property(x => x.Url).HasColumnName("url").IsRequired();
            e.Property(x => x.SortOrder).HasColumnName("sort_order");
            e.Property(x => x.IsMain).HasColumnName("is_main");
            e.Property(x => x.CreatedAtUtc).HasColumnName("created_at_utc");
        });

        // ===== Listing Attribute Values =====
        modelBuilder.Entity<ListingAttributeValue>(e =>
        {
            e.ToTable("listing_attribute_values", "public");
            e.HasKey(x => new { x.ListingId, x.AttributeId });

            e.Property(x => x.ListingId).HasColumnName("listing_id");
            e.Property(x => x.AttributeId).HasColumnName("attribute_id");

            e.Property(x => x.OptionId).HasColumnName("option_id");
            e.Property(x => x.ValueNumber).HasColumnName("value_number");
            e.Property(x => x.ValueText).HasColumnName("value_text");
            e.Property(x => x.ValueBool).HasColumnName("value_bool");

            e.HasOne(x => x.Listing)
                .WithMany(l => l.Attributes)
                .HasForeignKey(x => x.ListingId)
                .OnDelete(DeleteBehavior.Cascade);

            e.HasOne(x => x.Attribute)
                .WithMany()
                .HasForeignKey(x => x.AttributeId)
                .OnDelete(DeleteBehavior.Cascade);

            e.HasOne(x => x.Option)
                .WithMany()
                .HasForeignKey(x => x.OptionId)
                .OnDelete(DeleteBehavior.Restrict);
        });
    }
}
