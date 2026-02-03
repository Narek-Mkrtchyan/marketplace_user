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

        var urls = Environment.GetEnvironmentVariable("ASPNETCORE_URLS");
        if (string.IsNullOrWhiteSpace(urls))
        {
            builder.WebHost.UseUrls("http://localhost:5001");
        }


        builder.Services.AddControllers();
        builder.Services.AddEndpointsApiExplorer();
        builder.Services.AddSwaggerGen();
        builder.Services.AddDirectoryBrowser();
        builder.Services.AddDbContext<CatalogDbContext>(opt =>
        {
            opt.UseNpgsql(builder.Configuration.GetConnectionString("Catalog"));
        });

        builder.Services.Configure<JwtOptions>(builder.Configuration.GetSection("Jwt"));
        builder.Services.AddScoped<JwtTokenService>();
        builder.Services.AddHttpClient("UsersApi", client =>
        {
            client.BaseAddress = new Uri(builder.Configuration["Services:UsersApi"]!);
            client.Timeout = TimeSpan.FromSeconds(3);
        });

        // ===== JWT config read =====
        var jwtIssuer = builder.Configuration["Jwt:Issuer"] ?? throw new Exception("Jwt:Issuer is missing");
        var jwtAudience = builder.Configuration["Jwt:Audience"] ?? throw new Exception("Jwt:Audience is missing");
        var jwtKey = builder.Configuration["Jwt:Key"] ?? throw new Exception("Jwt:Key is missing");

        // ✅ DEBUG до Build()
        Console.WriteLine("ENV (builder) = " + (builder.Environment?.EnvironmentName ?? "<null>"));
        Console.WriteLine("Jwt:Issuer = " + jwtIssuer);
        Console.WriteLine("Jwt:Audience = " + jwtAudience);
        Console.WriteLine("Jwt:Key(from config) len = " + jwtKey.Length);
        Console.WriteLine("Jwt__Key(env) len = " + (Environment.GetEnvironmentVariable("Jwt__Key")?.Length ?? 0));
        Console.WriteLine("ASPNETCORE_ENVIRONMENT(env) = " + (Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "<null>"));
        Console.WriteLine("Jwt:Key head = " + jwtKey.Substring(0, Math.Min(5, jwtKey.Length)));
        Console.WriteLine("Jwt:Key tail = " + jwtKey.Substring(Math.Max(0, jwtKey.Length - 5)));


        builder.Services.AddAuthorization(options =>
        {
            options.AddPolicy("AdminOnly", p =>
                p.RequireAssertion(ctx =>
                {
                    var role = ctx.User.Claims.FirstOrDefault(c =>
                        c.Type == "role" || c.Type.EndsWith("/claims/role"))?.Value;

                    return string.Equals(role, "admin", StringComparison.OrdinalIgnoreCase)
                           || string.Equals(role, "Admin", StringComparison.OrdinalIgnoreCase);
                }));
        });

        builder.Services.AddCors(opt =>
        {
            opt.AddPolicy("Default", p => p
                .WithOrigins(
                    "https://dev.moll.am",
                    "http://localhost:5173",
                    "http://localhost:5174",
                    "http://127.0.0.1:5173",
                    "http://127.0.0.1:5174"
                )
                .AllowAnyHeader()
                .AllowAnyMethod()
                .AllowCredentials()
            );
        });

        var app = builder.Build();
        app.UseDeveloperExceptionPage();
        app.UseStaticFiles();

        app.Use(async (ctx, next) =>
        {
            try
            {
                await next();
            }
            catch (Exception ex)
            {
                Console.WriteLine(" UNHANDLED EXCEPTION:");
                Console.WriteLine(ex.ToString());
                throw;
            }
        });

        Console.WriteLine("ENV (app) = " + app.Environment.EnvironmentName);

        app.MapGet("/debug/token", () =>
        {
            var claims = new[]
            {
                new System.Security.Claims.Claim("sub", Guid.NewGuid().ToString()),
                new System.Security.Claims.Claim("email", "admin@moll.am"),
                new System.Security.Claims.Claim("role", "admin"),
            };

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var token = new System.IdentityModel.Tokens.Jwt.JwtSecurityToken(
                issuer: jwtIssuer,
                audience: jwtAudience,
                claims: claims,
                expires: DateTime.UtcNow.AddHours(2),
                signingCredentials: creds
            );

            return Results.Ok(new
            {
                token = new System.IdentityModel.Tokens.Jwt.JwtSecurityTokenHandler().WriteToken(token)
            });
        });

       
        app.MapPost("/api/admin/auth/login", (IConfiguration cfg, JwtTokenService jwt, AdminLoginRequest req) =>
        {
            var email = (req.Email ?? "").Trim().ToLowerInvariant();
            var password = req.Password ?? "";

            var adminEmail = (cfg["Admin:Email"] ?? "").Trim().ToLowerInvariant();
            var adminPass = cfg["Admin:Password"] ?? "";

            if (string.IsNullOrWhiteSpace(adminEmail) || string.IsNullOrWhiteSpace(adminPass))
                return Results.Problem("Admin credentials are not configured", statusCode: 500);

            if (email != adminEmail || password != adminPass)
                return Results.Unauthorized();

            var token = jwt.CreateAdminToken(email);
            return Results.Ok(new { token, email });
        });

        app.UseCors("Default");
        app.UseStaticFiles(); 

        if (app.Environment.IsDevelopment())
        {
            app.UseSwagger();
            app.UseSwaggerUI();
        }

        app.MapControllers();

        app.Run();
    }
}
