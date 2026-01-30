using System.Text;
using ListamCompetitor.Api.Auth;
using ListamCompetitor.Api.Data;
using ListamCompetitor.Api.Models;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.Extensions.FileProviders;

var builder = WebApplication.CreateBuilder(args);

// Controllers
builder.Services.AddControllers();

// Swagger
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.Configure<JwtOptions>(builder.Configuration.GetSection("Jwt"));
builder.Services.AddSingleton<JwtTokenService>();

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("Default")));

builder.Services.AddCors(opt =>
{
    opt.AddDefaultPolicy(p => p
        .WithOrigins(
            "https://dev.moll.am",
            "http://localhost:5173",
            "http://127.0.0.1:5173"
        )
        .AllowAnyHeader()
        .AllowAnyMethod()
        .AllowCredentials()
    );
});

// Mail
builder.Services.Configure<MailSettings>(builder.Configuration.GetSection("Mail"));
builder.Services.AddScoped<IMailService, SmtpMailService>();

// JWT auth
var jwt = builder.Configuration.GetSection("Jwt").Get<JwtOptions>() ?? new JwtOptions();

Console.WriteLine($"ENV={builder.Environment.EnvironmentName}");
Console.WriteLine($"JWT Issuer={jwt.Issuer}, Audience={jwt.Audience}, KeyLen={(jwt.Key ?? "").Length}");

if (string.IsNullOrWhiteSpace(jwt.Key))
    throw new Exception("Jwt:Key is empty. Check appsettings / environment variables.");
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

        opt.Events = new JwtBearerEvents
        {
            OnMessageReceived = ctx =>
            {
                var auth = ctx.Request.Headers.Authorization.ToString();
                Console.WriteLine($"[JWT] {ctx.Request.Method} {ctx.Request.Path} AuthHeader=" +
                                  (string.IsNullOrWhiteSpace(auth) ? "<EMPTY>" : auth[..Math.Min(35, auth.Length)] + "..."));
                return Task.CompletedTask;
            },
            OnAuthenticationFailed = ctx =>
            {
                Console.WriteLine("[JWT] AuthenticationFailed: " + ctx.Exception.Message);
                return Task.CompletedTask;
            },
            OnChallenge = ctx =>
            {
                Console.WriteLine("[JWT] Challenge: " + (ctx.Error ?? "<no error>") + " | " + (ctx.ErrorDescription ?? "<no desc>"));
                return Task.CompletedTask;
            }
        };
    });


builder.Services.AddAuthorization();

var app = builder.Build();
Console.WriteLine("APP ContentRootPath = " + app.Environment.ContentRootPath);
Console.WriteLine("APP WebRootPath     = " + app.Environment.WebRootPath);

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}
app.UseStaticFiles();
app.UseStaticFiles(new StaticFileOptions
{
    FileProvider = new PhysicalFileProvider(
        Path.Combine(app.Environment.WebRootPath!, "uploads")
    ),
    RequestPath = "/uploads"
});
app.UseCors();
app.UseAuthentication();
app.UseAuthorization();

// Controllers mapping
app.MapControllers();

app.Run();
