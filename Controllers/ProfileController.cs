using System.Security.Claims;
using ListamCompetitor.Api.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ListamCompetitor.Api.Controllers;

[ApiController]
[Authorize]
[Route("profile")]
public class ProfileController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly IWebHostEnvironment _env;

    public ProfileController(AppDbContext db, IWebHostEnvironment env)
    {
        _db = db;
        _env = env;
    }

    // ✅ PUT /profile
    [HttpPut]
    public async Task<IActionResult> Update([FromBody] UpdateProfileRequest req)
    {
        var email = User.FindFirstValue(ClaimTypes.Email) ?? User.FindFirstValue("email");
        if (email is null) return Unauthorized();

        var user = await _db.Users.FirstOrDefaultAsync(x => x.Email == email);
        if (user is null) return Unauthorized();

        // Меняем только эти поля (email не трогаем)
        user.FirstName = (req.FirstName ?? "").Trim();
        user.LastName  = (req.LastName  ?? "").Trim();
        user.Phone     = (req.Phone     ?? "").Trim();
        user.Gender    = (req.Gender    ?? "").Trim();
        user.Country   = (req.Country   ?? "").Trim();
        user.City      = (req.City      ?? "").Trim();

        await _db.SaveChangesAsync();

        return Ok(new
        {
            ok = true,
            user = new
            {
                user.Email,
                user.FirstName,
                user.LastName,
                user.Phone,
                user.Gender,
                user.Country,
                user.City,
                user.PhotoUrl
            }
        });
    }

    // ✅ POST /profile/photo
    [HttpPost("photo")]
    [RequestSizeLimit(5 * 1024 * 1024)]
    public async Task<IActionResult> UploadPhoto([FromForm] IFormFile file)
    {
        if (file == null || file.Length == 0)
            return BadRequest(new { message = "File is required" });

        var allowed = new[] { "image/jpeg", "image/png", "image/webp" };
        if (!allowed.Contains(file.ContentType))
            return BadRequest(new { message = "Only JPG/PNG/WebP allowed" });

        if (file.Length > 5 * 1024 * 1024)
            return BadRequest(new { message = "Max file size is 5MB" });

        var email = User.FindFirstValue(ClaimTypes.Email) ?? User.FindFirstValue("email");
        if (email is null) return Unauthorized();

        var user = await _db.Users.FirstOrDefaultAsync(x => x.Email == email);
        if (user is null) return Unauthorized();

        // ✅ если wwwroot отсутствует, WebRootPath может быть null
        var webRoot = _env.WebRootPath;
        if (string.IsNullOrWhiteSpace(webRoot))
        {
            webRoot = Path.Combine(_env.ContentRootPath, "wwwroot");
        }

        var uploadsDir = Path.Combine(webRoot, "uploads");
        Directory.CreateDirectory(uploadsDir);

        var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (string.IsNullOrWhiteSpace(ext)) ext = ".jpg";

        var fileName = $"u_{user.Id}_{Guid.NewGuid():N}{ext}";
        var fullPath = Path.Combine(uploadsDir, fileName);

        await using (var stream = System.IO.File.Create(fullPath))
        {
            await file.CopyToAsync(stream);
        }

        user.PhotoUrl = $"/uploads/{fileName}";
        await _db.SaveChangesAsync();

        return Ok(new { ok = true, photoUrl = user.PhotoUrl });
    }

    public record UpdateProfileRequest(
        string? FirstName,
        string? LastName,
        string? Phone,
        string? Gender,
        string? Country,
        string? City
    );
}
