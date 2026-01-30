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

// -------------------- CONFIG / SERVICES --------------------

builder.Services.Configure<JwtOptions>(builder.Configuration.GetSection("Jwt"));
builder.Services.AddSingleton<JwtTokenService>();

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("Default")));

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

// Swagger
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Mail
builder.Services.Configure<MailSettings>(builder.Configuration.GetSection("Mail"));
builder.Services.AddScoped<IMailService, SmtpMailService>();

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

// -------------------- MIDDLEWARE --------------------

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors();
app.UseAuthentication();
app.UseAuthorization();

// -------------------- SEED DB --------------------

using (var scope = app.Services.CreateScope())
{
    var cs = builder.Configuration.GetConnectionString("Default");
    Console.WriteLine("EF ConnectionString = " + cs);

}
// using (var scope = app.Services.CreateScope())
// {
//     var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
//     try
//     {
//         await DbSeed.EnsureSeededAsync(db);
//     }
//     catch (Exception ex)
//     {
//         Console.WriteLine("DB seed failed: " + ex.Message);
//     }
// }

// -------------------- ENDPOINTS --------------------

app.MapGet("/health", () => Results.Ok(new { ok = true }));

// ---------- AUTH: SEND CODE ----------
app.MapPost("/auth/register/code", async (AppDbContext db, IMailService mail, SendRegisterCodeRequest req) =>
{
    var email = (req.Email ?? "").Trim().ToLowerInvariant();
    if (string.IsNullOrWhiteSpace(email))
        return Results.BadRequest(new { message = "Email is required" });

    if (await db.Users.AnyAsync(x => x.Email == email))
        return Results.Conflict(new { message = "User already exists" });

    var code = Random.Shared.Next(100000, 999999).ToString();

    db.EmailVerificationCodes.Add(new EmailVerificationCode
    {
        Id = Guid.NewGuid(),
        Email = email,
        Code = code,
        CreatedAt = DateTime.UtcNow,
        ExpiresAt = DateTime.UtcNow.AddMinutes(10),
        Used = false
    });

    await db.SaveChangesAsync();

    try
    {
        await mail.SendAsync(
            email,
            "Marketplace verification code",
            $"<h2>Your code: {code}</h2><p>Valid for 10 minutes.</p>"
        );
    }
    catch (Exception ex)
    {
       
        return Results.BadRequest(new { message = $"Mail error: {ex.Message}" });
    }

    return Results.Ok(new { ok = true });
});

// ---------- AUTH: COMPLETE REGISTER ----------
app.MapPost("/auth/register/complete", async (AppDbContext db, JwtTokenService jwtSvc, CompleteRegisterRequest req) =>
{
    var email = (req.Email ?? "").Trim().ToLowerInvariant();
    if (string.IsNullOrWhiteSpace(email))
        return Results.BadRequest(new { message = "Email is required" });

    if (string.IsNullOrWhiteSpace(req.Password) || req.Password.Length < 6)
        return Results.BadRequest(new { message = "Password must be at least 6 characters" });

    if (string.IsNullOrWhiteSpace(req.Code))
        return Results.BadRequest(new { message = "Code is required" });

    if (string.IsNullOrWhiteSpace(req.FirstName) ||
        string.IsNullOrWhiteSpace(req.LastName) ||
        string.IsNullOrWhiteSpace(req.Phone) ||
        string.IsNullOrWhiteSpace(req.Gender) ||
        string.IsNullOrWhiteSpace(req.Country) ||
        string.IsNullOrWhiteSpace(req.City))
    {
        return Results.BadRequest(new { message = "All profile fields are required" });
    }

    var record = await db.EmailVerificationCodes
        .Where(x => x.Email == email && x.Code == req.Code && !x.Used && x.ExpiresAt > DateTime.UtcNow)
        .OrderByDescending(x => x.CreatedAt)
        .FirstOrDefaultAsync();

    if (record is null)
        return Results.BadRequest(new { message = "Invalid or expired code" });

    if (await db.Users.AnyAsync(x => x.Email == email))
        return Results.Conflict(new { message = "User already exists" });

    var user = new User
    {
        Email = email,

        FirstName = req.FirstName.Trim(),
        LastName = req.LastName.Trim(),
        Phone = req.Phone.Trim(),
        Gender = req.Gender.Trim(),   // "man"/"woman"
        Country = req.Country.Trim(),
        City = req.City.Trim(),
    };

    var hasher = new PasswordHasher<User>();
    user.PasswordHash = hasher.HashPassword(user, req.Password);

    record.Used = true;

    db.Users.Add(user);
    await db.SaveChangesAsync();

    var token = jwtSvc.CreateToken(user);
    return Results.Ok(new { token, email = user.Email });
});

// ---------- AUTH: LOGIN ----------
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

// ---------- ME (protected) ----------
app.MapGet("/me", async (ClaimsPrincipal principal, AppDbContext db) =>
{
    var email = principal.FindFirstValue(ClaimTypes.Email) ?? principal.FindFirstValue("email");
    if (email is null) return Results.Unauthorized();

    var user = await db.Users.AsNoTracking().FirstOrDefaultAsync(x => x.Email == email);
    if (user is null) return Results.Unauthorized();

    return Results.Ok(new
    {
        id = user.Id,
        email = user.Email,
        firstName = user.FirstName,
        lastName = user.LastName,
        phone = user.Phone,
        gender = user.Gender,
        country = user.Country,
        city = user.City,
        createdAtUtc = user.CreatedAtUtc
    });
}).RequireAuthorization();

// ---------- LISTINGS ----------
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

// -------------------- REQUEST DTOS --------------------

public record SendRegisterCodeRequest(string Email);

public record CompleteRegisterRequest(
    string Email,
    string Code,
    string Password,
    string FirstName,
    string LastName,
    string Phone,
    string Gender,
    string Country,
    string City
);

public record LoginRequest(string Email, string Password);
