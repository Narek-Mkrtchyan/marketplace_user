using Microsoft.EntityFrameworkCore;
using ListamCompetitor.Api.Models;
using ListamCompetitor.Api.Models;

namespace ListamCompetitor.Api.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) {}

    public DbSet<User> Users => Set<User>();
    public DbSet<Listing> Listings => Set<Listing>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<User>()
            .HasIndex(x => x.Email)
            .IsUnique();

        modelBuilder.Entity<User>()
            .Property(x => x.Email)
            .HasMaxLength(320);

        modelBuilder.Entity<Listing>()
            .Property(x => x.Title)
            .HasMaxLength(200);

        base.OnModelCreating(modelBuilder);
    }
    public DbSet<EmailVerificationCode> EmailVerificationCodes => Set<EmailVerificationCode>();

}
