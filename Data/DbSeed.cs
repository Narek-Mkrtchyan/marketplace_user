using ListamCompetitor.Api.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace ListamCompetitor.Api.Data;

public static class DbSeed
{
    public static async Task EnsureSeededAsync(AppDbContext db)
    {
        await db.Database.EnsureCreatedAsync();

        if (!await db.Users.AnyAsync())
        {
            var hasher = new PasswordHasher<User>();
            var demo = new User { Email = "demo@ban.com" };
            demo.PasswordHash = hasher.HashPassword(demo, "Password1!");
            db.Users.Add(demo);
            await db.SaveChangesAsync();
        }

        if (!await db.Listings.AnyAsync())
        {
            var demoEmail = (await db.Users.AsNoTracking().FirstAsync()).Email;

            db.Listings.AddRange(
                new Listing { Title = "Mazda 3 2020", Price = 6500000, City = "Yerevan", Description = "Clean car, one owner.", OwnerEmail = demoEmail },
                new Listing { Title = "2-room apartment", Price = 35000000, City = "Yerevan", Description = "Center, renovated.", OwnerEmail = demoEmail },
                new Listing { Title = "Web development services", Price = 150000, City = "Remote", Description = "React + .NET backend.", OwnerEmail = demoEmail }
            );
            await db.SaveChangesAsync();
        }
    }
}
