using System.Security.Claims;
using System.Text;
using ListamCompetitor.Api.Auth;
using ListamCompetitor.Api.Data;
using ListamCompetitor.Api.Models;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

var builder = WebApplication.CreateBuilder(args);

builder.Services.Configure<JwtOptions>(builder.Configuration.GetSection("Jwt"));
builder.Services.AddSingleton<JwtTokenService>();

builder.Services.AddDbContext<AppDbContext>(opt =>
{
    var cs = builder.Configuration.GetConnectionString("Default");
    opt.UseNpgsql(cs);
});

builder.Services.AddCors(opt =>
{
    opt.AddDefaultPolicy(p => p
        .WithOrigins(
            "http://localhost:5173",
            "http://127.0.0.1:5173"
        )
        .AllowAnyHeader()
        .AllowAnyMethod()
        .AllowCredentials()
    );
});

// JWT auth
var jwt = builder.Configuration.GetSection("Jwt").Get<JwtOptions>() ?? new JwtOptions();
var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt.Key));

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(opt =>
    {
        opt.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateIssuerSigningKey = true,
            ValidateLifetime = true,
            ValidIssuer = jwt.Issuer,
            ValidAudience = jwt.Audience,
            IssuerSigningKey = key,
            ClockSkew = TimeSpan.FromMinutes(1)
        };
    });

builder.Services.AddAuthorization();

var app = builder.Build();

app.UseCors();
app.UseAuthentication();
app.UseAuthorization();

// seed DB
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    await DbSeed.EnsureSeededAsync(db);
}

app.MapGet("/health", () => Results.Ok(new { ok = true }));

// --- AUTH ---
app.MapPost("/auth/register", async (AppDbContext db, JwtTokenService jwtSvc, RegisterRequest req) =>
{
    var email = (req.Email ?? "").Trim().ToLowerInvariant();
    if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(req.Password))
        return Results.BadRequest(new { message = "Email and password are required" });
    if (req.Password.Length < 6)
        return Results.BadRequest(new { message = "Password must be at least 6 characters" });

    if (await db.Users.AnyAsync(x => x.Email == email))
        return Results.Conflict(new { message = "User already exists" });

    var user = new User { Email = email };
    var hasher = new PasswordHasher<User>();
    user.PasswordHash = hasher.HashPassword(user, req.Password);

    db.Users.Add(user);
    await db.SaveChangesAsync();

    var token = jwtSvc.CreateToken(user);
    return Results.Ok(new { token, email = user.Email });
});

app.MapPost("/auth/login", async (AppDbContext db, JwtTokenService jwtSvc, LoginRequest req) =>
{
    var email = (req.Email ?? "").Trim().ToLowerInvariant();
    if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(req.Password))
        return Results.BadRequest(new { message = "Email and password are required" });

    var user = await db.Users.FirstOrDefaultAsync(x => x.Email == email);
    if (user is null)
        return Results.Unauthorized();

    var hasher = new PasswordHasher<User>();
    var vr = hasher.VerifyHashedPassword(user, user.PasswordHash, req.Password);
    if (vr == PasswordVerificationResult.Failed)
        return Results.Unauthorized();

    var token = jwtSvc.CreateToken(user);
    return Results.Ok(new { token, email = user.Email });
});

app.MapGet("/me", async (ClaimsPrincipal principal, AppDbContext db) =>
{
    var email = principal.FindFirstValue(ClaimTypes.Email) ?? principal.FindFirstValue("email");
    if (email is null) return Results.Unauthorized();

    var user = await db.Users.AsNoTracking().FirstOrDefaultAsync(x => x.Email == email);
    if (user is null) return Results.Unauthorized();

    return Results.Ok(new { id = user.Id, email = user.Email, createdAtUtc = user.CreatedAtUtc });
}).RequireAuthorization();

// --- MOCK DATA / LISTINGS ---
app.MapGet("/listings", async (AppDbContext db, string? q, string? city, decimal? minPrice, decimal? maxPrice) =>
{
    var query = db.Listings.AsNoTracking().AsQueryable();

    if (!string.IsNullOrWhiteSpace(q))
        query = query.Where(x => x.Title.Contains(q) || x.Description.Contains(q));

    if (!string.IsNullOrWhiteSpace(city))
        query = query.Where(x => x.City == city);

    if (minPrice is not null)
        query = query.Where(x => x.Price >= minPrice);

    if (maxPrice is not null)
        query = query.Where(x => x.Price <= maxPrice);

    var items = await query
        .OrderByDescending(x => x.CreatedAtUtc)
        .Take(50)
        .Select(x => new
        {
            x.Id,
            x.Title,
            x.Price,
            x.City,
            x.OwnerEmail,
            x.CreatedAtUtc
        })
        .ToListAsync();

    return Results.Ok(items);
});

app.MapGet("/listings/{id:int}", async (AppDbContext db, int id) =>
{
    var x = await db.Listings.AsNoTracking().FirstOrDefaultAsync(l => l.Id == id);
    return x is null ? Results.NotFound() : Results.Ok(x);
});

app.Run();

public record RegisterRequest(string Email, string Password);
public record LoginRequest(string Email, string Password);
