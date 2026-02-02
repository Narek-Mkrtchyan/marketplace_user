using System.Text;
using Catalog.Api.Auth;
using Catalog.Api.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

namespace Catalog.Api;

public record AdminLoginRequest(string Email, string Password);

public class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        // ===== URL (container listens on 8080) =====
        // ВАЖНО: если это mp_api в Docker – НЕ 5001
        builder.WebHost.UseUrls("http://0.0.0.0:8080");

        // ===== SERVICES =====
        builder.Services.AddControllers();
        builder.Services.AddEndpointsApiExplorer();
        builder.Services.AddSwaggerGen();

        builder.Services.AddDbContext<CatalogDbContext>(opt =>
        {
            opt.UseNpgsql(builder.Configuration.GetConnectionString("Default"));
        });

        // ===== JWT =====
        builder.Services.Configure<JwtOptions>(
            builder.Configuration.GetSection("Jwt"));

        builder.Services.AddScoped<JwtTokenService>();

        var jwtIssuer = builder.Configuration["Jwt:Issuer"]
            ?? throw new Exception("Jwt:Issuer is missing");

        var jwtAudience = builder.Configuration["Jwt:Audience"]
            ?? throw new Exception("Jwt:Audience is missing");

        var jwtKey = builder.Configuration["Jwt:Key"]
            ?? throw new Exception("Jwt:Key is missing");

        // ===== AUTH =====
        builder.Services.AddAuthentication("Bearer")
            .AddJwtBearer("Bearer", opt =>
            {
                opt.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateIssuerSigningKey = true,
                    ValidateLifetime = true,

                    ValidIssuer = jwtIssuer,
                    ValidAudience = jwtAudience,
                    IssuerSigningKey =
                        new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey))
                };
            });

        builder.Services.AddAuthorization();

        builder.Services.AddCors(opt =>
        {
            opt.AddPolicy("Default", p =>
                p.WithOrigins(
                        "https://dev.moll.am",
                        "https://api.dev.moll.am",
                        "http://localhost:5173",
                        "http://localhost:5174"
                    )
                    .AllowAnyHeader()
                    .AllowAnyMethod()
                    .AllowCredentials()
            );
        });

        var app = builder.Build();

        // ===== PIPELINE (ПОРЯДОК ВАЖЕН) =====

        if (app.Environment.IsDevelopment())
        {
            app.UseDeveloperExceptionPage();
            app.UseSwagger();
            app.UseSwaggerUI();
        }

        app.UseRouting();

        app.UseCors("Default");

        app.UseAuthentication();
        app.UseAuthorization();

        // ===== ENDPOINTS =====

        // health
        app.MapGet("/health", () => Results.Ok(new { ok = true }));

        // debug token
        app.MapGet("/debug/token", () =>
        {
            var claims = new[]
            {
                new System.Security.Claims.Claim("sub", Guid.NewGuid().ToString()),
                new System.Security.Claims.Claim("email", "debug@moll.am"),
                new System.Security.Claims.Claim("role", "user"),
            };

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var token = new System.IdentityModel.Tokens.Jwt.JwtSecurityToken(
                issuer: jwtIssuer,
                audience: jwtAudience,
                claims: claims,
                expires: DateTime.UtcNow.AddHours(1),
                signingCredentials: creds
            );

            return Results.Ok(new
            {
                token = new System.IdentityModel.Tokens.Jwt.JwtSecurityTokenHandler()
                    .WriteToken(token)
            });
        });

        // admin login
        app.MapPost("/api/admin/auth/login",
            (IConfiguration cfg, JwtTokenService jwt, AdminLoginRequest req) =>
            {
                var email = (req.Email ?? "").Trim().ToLowerInvariant();
                var password = req.Password ?? "";

                var adminEmail = (cfg["Admin:Email"] ?? "")
                    .Trim().ToLowerInvariant();

                var adminPass = cfg["Admin:Password"] ?? "";

                if (email != adminEmail || password != adminPass)
                    return Results.Unauthorized();

                var token = jwt.CreateAdminToken(email);
                return Results.Ok(new { token, email });
            });

        // controllers (/api/auth/google и т.д.)
        app.MapControllers();

        app.Run();
    }
}
