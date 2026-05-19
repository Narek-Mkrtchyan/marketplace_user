using System.Text;
using ListamCompetitor.Api.Auth;
using ListamCompetitor.Api.Data;
using ListamCompetitor.Api.Models;
using ListamCompetitor.Api.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.FileProviders;
using Microsoft.IdentityModel.Tokens;

var builder = WebApplication.CreateBuilder(args);

// -------------------- SERVICES --------------------

builder.Services.AddControllers();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("Default")));

builder.Services.Configure<JwtOptions>(builder.Configuration.GetSection("Jwt"));
builder.Services.AddSingleton<JwtTokenService>();

builder.Services.AddScoped<IReviewsService, ReviewsService>();

builder.Services.Configure<MailSettings>(builder.Configuration.GetSection("Mail"));
builder.Services.AddScoped<IMailService, SmtpMailService>();

// -------------------- CORS --------------------

builder.Services.AddCors(options =>
{
    options.AddPolicy("Default", policy =>
    {
        policy
            .WithOrigins(
                "https://dev.moll.am",
                "https://moll.am",

                "http://localhost:5173",
                "https://localhost:5173",
                "http://localhost:5174",
                "https://localhost:5174",

                "http://127.0.0.1:5173",
                "https://127.0.0.1:5173",
                "http://127.0.0.1:5174",
                "https://127.0.0.1:5174",

                "http://localhost:5001",
                "https://localhost:5001",
                "http://127.0.0.1:5001",
                "https://127.0.0.1:5001",
                "http://127.0.0.1:5002",
                "https://127.0.0.1:5002"
            )
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials();
    });
});

// -------------------- JWT AUTH --------------------

var jwt = builder.Configuration.GetSection("Jwt").Get<JwtOptions>() ?? new JwtOptions();

Console.WriteLine($"ENV={builder.Environment.EnvironmentName}");
Console.WriteLine($"JWT Issuer={jwt.Issuer}, Audience={jwt.Audience}, KeyLen={(jwt.Key ?? "").Length}");

if (string.IsNullOrWhiteSpace(jwt.Key))
    throw new Exception("Jwt:Key is empty. Check appsettings / environment variables.");

var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt.Key));

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
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

        options.Events = new JwtBearerEvents
        {
            OnMessageReceived = context =>
            {
                var auth = context.Request.Headers.Authorization.ToString();

                Console.WriteLine($"[JWT] {context.Request.Method} {context.Request.Path} AuthHeader=" +
                                  (string.IsNullOrWhiteSpace(auth)
                                      ? "<EMPTY>"
                                      : auth[..Math.Min(35, auth.Length)] + "..."));

                return Task.CompletedTask;
            },
            OnAuthenticationFailed = context =>
            {
                Console.WriteLine("[JWT] AuthenticationFailed: " + context.Exception.Message);
                return Task.CompletedTask;
            },
            OnChallenge = context =>
            {
                Console.WriteLine("[JWT] Challenge: " + (context.Error ?? "<no error>") +
                                  " | " + (context.ErrorDescription ?? "<no desc>"));
                return Task.CompletedTask;
            }
        };
    });

builder.Services.AddAuthorization();

// -------------------- APP --------------------

var app = builder.Build();

Console.WriteLine("APP ContentRootPath = " + app.Environment.ContentRootPath);
Console.WriteLine("APP WebRootPath     = " + app.Environment.WebRootPath);

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// -------------------- STATIC FILES --------------------

var webRoot = app.Environment.WebRootPath;

if (string.IsNullOrWhiteSpace(webRoot))
{
    webRoot = Path.Combine(app.Environment.ContentRootPath, "wwwroot");
    Directory.CreateDirectory(webRoot);
}

app.UseStaticFiles();

var uploadsPath = Path.Combine(webRoot, "uploads");
Directory.CreateDirectory(uploadsPath);

app.UseStaticFiles(new StaticFileOptions
{
    FileProvider = new PhysicalFileProvider(uploadsPath),
    RequestPath = "/uploads"
});

// -------------------- PIPELINE --------------------

app.UseRouting();

app.UseCors("Default");

app.UseAuthentication();
app.UseAuthorization();

app.MapGet("/health", () => Results.Ok(new { ok = true }));

app.MapControllers();

app.Run();