using System.Security.Claims;
using System.Text.Json;
using ListamCompetitor.Api.Auth;
using ListamCompetitor.Api.Data;
using ListamCompetitor.Api.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ListamCompetitor.Api.Controllers;

[ApiController]
[Route("auth")]
public class AuthController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly IMailService _mail;
    private readonly JwtTokenService _jwtSvc;

    public AuthController(AppDbContext db, IMailService mail, JwtTokenService jwtSvc)
    {
        _db = db;
        _mail = mail;
        _jwtSvc = jwtSvc;
    }
    
    public record GoogleAuthRequest(string IdToken);
    [HttpPost("google")]
    public async Task<IActionResult> Google([FromBody] GoogleAuthRequest req)
    {
        if (string.IsNullOrWhiteSpace(req.IdToken))
            return BadRequest(new { message = "idToken is required" });

        // Проверяем id_token через Google tokeninfo
        using var client = new HttpClient();
        var json = await client.GetStringAsync(
            $"https://oauth2.googleapis.com/tokeninfo?id_token={req.IdToken}"
        );

        var payload = JsonSerializer.Deserialize<GoogleTokenInfo>(json);
        if (payload?.Email is null)
            return Unauthorized();

        // ВАЖНО: проверим, что токен для твоего клиента (aud)
        // сюда вставь твой Google Client ID
        var expectedAud = Environment.GetEnvironmentVariable("GOOGLE_CLIENT_ID") ?? "";
        if (!string.IsNullOrWhiteSpace(expectedAud) && payload.Aud != expectedAud)
            return Unauthorized(new { message = "Invalid Google token audience" });

        var email = payload.Email.Trim().ToLowerInvariant();

        var user = await _db.Users.FirstOrDefaultAsync(x => x.Email == email);
        if (user == null)
        {
            user = new User
            {
                Email = email,
                FirstName = payload.GivenName,
                LastName = payload.FamilyName,
                PhotoUrl = payload.Picture,       // можно сохранить гугл-аватар
                CreatedAtUtc = DateTime.UtcNow,
                PasswordHash = ""                 // пароль не нужен
            };

            _db.Users.Add(user);
            await _db.SaveChangesAsync();
        }

        var token = _jwtSvc.CreateToken(user);
        return Ok(new { token, email = user.Email });
    }

    private class GoogleTokenInfo
    {
        public string? Email { get; set; }
        public string? Aud { get; set; }
        public string? GivenName { get; set; }
        public string? FamilyName { get; set; }
        public string? Picture { get; set; }
    }
    // POST /auth/register/code
    [HttpPost("register/code")]
    public async Task<IActionResult> SendRegisterCode([FromBody] SendRegisterCodeRequest req)
    {
        var email = (req.Email ?? "").Trim().ToLowerInvariant();
        if (string.IsNullOrWhiteSpace(email))
            return BadRequest(new { message = "Email is required" });

        if (await _db.Users.AnyAsync(x => x.Email == email))
            return Conflict(new { message = "User already exists" });

        var code = Random.Shared.Next(100000, 999999).ToString();

        _db.EmailVerificationCodes.Add(new EmailVerificationCode
        {
            Id = Guid.NewGuid(),
            Email = email,
            Code = code,
            CreatedAt = DateTime.UtcNow,
            ExpiresAt = DateTime.UtcNow.AddMinutes(10),
            Used = false
        });

        await _db.SaveChangesAsync();

        try
        {
            await _mail.SendAsync(
                email,
                "Marketplace verification code",
                $"<h2>Your code: {code}</h2><p>Valid for 10 minutes.</p>"
            );
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = $"Mail error: {ex.Message}" });
        }

        return Ok(new { ok = true });
    }

    // POST /auth/register/complete
    [HttpPost("register/complete")]
    public async Task<IActionResult> CompleteRegister([FromBody] CompleteRegisterRequest req)
    {
        var email = (req.Email ?? "").Trim().ToLowerInvariant();
        if (string.IsNullOrWhiteSpace(email))
            return BadRequest(new { message = "Email is required" });

        if (string.IsNullOrWhiteSpace(req.Password) || req.Password.Length < 6)
            return BadRequest(new { message = "Password must be at least 6 characters" });

        if (string.IsNullOrWhiteSpace(req.Code))
            return BadRequest(new { message = "Code is required" });

        if (string.IsNullOrWhiteSpace(req.FirstName) ||
            string.IsNullOrWhiteSpace(req.LastName) ||
            string.IsNullOrWhiteSpace(req.Phone) ||
            string.IsNullOrWhiteSpace(req.Gender) ||
            string.IsNullOrWhiteSpace(req.Country) ||
            string.IsNullOrWhiteSpace(req.City))
        {
            return BadRequest(new { message = "All profile fields are required" });
        }

        var record = await _db.EmailVerificationCodes
            .Where(x => x.Email == email && x.Code == req.Code && !x.Used && x.ExpiresAt > DateTime.UtcNow)
            .OrderByDescending(x => x.CreatedAt)
            .FirstOrDefaultAsync();

        if (record is null)
            return BadRequest(new { message = "Invalid or expired code" });

        if (await _db.Users.AnyAsync(x => x.Email == email))
            return Conflict(new { message = "User already exists" });

        var user = new User
        {
            Email = email,
            FirstName = req.FirstName.Trim(),
            LastName = req.LastName.Trim(),
            Phone = req.Phone.Trim(),
            Gender = req.Gender.Trim(),
            Country = req.Country.Trim(),
            City = req.City.Trim(),
        };

        var hasher = new PasswordHasher<User>();
        user.PasswordHash = hasher.HashPassword(user, req.Password);

        record.Used = true;

        _db.Users.Add(user);
        await _db.SaveChangesAsync();

        var token = _jwtSvc.CreateToken(user);
        return Ok(new { token, email = user.Email });
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest req)
    {
        var email = (req.Email ?? "").Trim().ToLowerInvariant();
        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(req.Password))
            return BadRequest(new { message = "Email and password are required" });

        var user = await _db.Users.FirstOrDefaultAsync(x => x.Email == email);
        if (user is null)
            return Unauthorized();

        var hasher = new PasswordHasher<User>();
        var vr = hasher.VerifyHashedPassword(user, user.PasswordHash, req.Password);
        if (vr == PasswordVerificationResult.Failed)
            return Unauthorized();

        var token = _jwtSvc.CreateToken(user);
        return Ok(new { token, email = user.Email });
    }

    [Authorize]
    [HttpGet("me")]
    public async Task<IActionResult> Me()
    {
        var email = User.FindFirstValue(ClaimTypes.Email) ?? User.FindFirstValue("email");
        if (email is null) return Unauthorized();

        var user = await _db.Users.AsNoTracking().FirstOrDefaultAsync(x => x.Email == email);
        if (user is null) return Unauthorized();

        return Ok(new
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
    }

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
}
