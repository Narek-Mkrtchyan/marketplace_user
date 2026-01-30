using System.Security.Claims;
using ListamCompetitor.Api.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ListamCompetitor.Api.Controllers;

[ApiController]
[Authorize]
[Route("me")]
public class MeController : ControllerBase
{
    private readonly AppDbContext _db;

    public MeController(AppDbContext db) => _db = db;

    [HttpGet]
    public async Task<IActionResult> Get()
    {
        var email = User.FindFirstValue(ClaimTypes.Email) ?? User.FindFirstValue("email");
        if (string.IsNullOrWhiteSpace(email)) return Unauthorized();

        var user = await _db.Users.AsNoTracking().FirstOrDefaultAsync(x => x.Email == email);
        if (user is null) return Unauthorized();

        string? photoUrl = null;
        if (!string.IsNullOrWhiteSpace(user.PhotoUrl))
        {
            if (user.PhotoUrl.StartsWith("http", StringComparison.OrdinalIgnoreCase))
                photoUrl = user.PhotoUrl;
            else
                photoUrl = $"{Request.Scheme}://{Request.Host}{user.PhotoUrl}";
        }

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
            createdAtUtc = user.CreatedAtUtc,
            photoUrl
        });
    }
}