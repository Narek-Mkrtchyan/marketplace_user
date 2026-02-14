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
            x.Property(r => r.Id)
                .HasColumnName("id")
                .HasDefaultValueSql("gen_random_uuid()");

            x.Property(r => r.AuthorId)
                .HasColumnName("author_id")
                .IsRequired();

            x.Property(r => r.TargetUserId)
                .HasColumnName("target_user_id")
                .IsRequired();

            x.Property(r => r.Rating)
                .HasColumnName("rating")
                .IsRequired();

            x.Property(r => r.Comment)
                .HasColumnName("comment");

            x.Property(r => r.CreatedAtUtc)
                .HasColumnName("created_at_utc")
                .HasDefaultValueSql("now()")
                .IsRequired();

            x.HasIndex(r => r.TargetUserId).HasDatabaseName("idx_reviews_target_user_id");
            x.HasIndex(r => r.AuthorId).HasDatabaseName("idx_reviews_author_id");

            x.HasCheckConstraint("ck_reviews_rating", "rating >= 1 AND rating <= 5");
            x.HasCheckConstraint("ck_reviews_no_self", "author_id <> target_user_id");
        });


        base.OnModelCreating(modelBuilder);
    }
    public DbSet<EmailVerificationCode> EmailVerificationCodes => Set<EmailVerificationCode>();

}
