using Microsoft.EntityFrameworkCore;
using ListamCompetitor.Api.Models;
using ListamCompetitor.Api.Models;

namespace ListamCompetitor.Api.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) {}

    public DbSet<User> Users => Set<User>();
    public DbSet<Listing> Listings => Set<Listing>();
    public DbSet<Review> Reviews => Set<Review>();
    
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<User>()
            .HasIndex(x => x.Email)
            .IsUnique();

        modelBuilder.Entity<User>()
            .Property(x => x.Email)
            .HasMaxLength(320);

        modelBuilder.Entity<User>()
            .Property(x => x.Id)
            .ValueGeneratedOnAdd()
            .HasDefaultValueSql("gen_random_uuid()");

        modelBuilder.Entity<Listing>()
            .Property(x => x.Title)
            .HasMaxLength(200);
        
        modelBuilder.Entity<Review>(x =>
        {
            x.ToTable("reviews");
            x.HasKey(r => r.Id);

            x.Property(r => r.Rating)
                .IsRequired();

            x.HasIndex(r => r.TargetUserId);
            x.HasIndex(r => r.AuthorId);

            x.HasCheckConstraint(
                "ck_reviews_rating",
                "\"Rating\" >= 1 AND \"Rating\" <= 5");

            x.HasCheckConstraint(
                "ck_reviews_no_self",
                "\"AuthorId\" <> \"TargetUserId\"");
        });

        base.OnModelCreating(modelBuilder);
    }
    public DbSet<EmailVerificationCode> EmailVerificationCodes => Set<EmailVerificationCode>();

}
