using System.Security.Claims;
using ListamCompetitor.Api.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ListamCompetitor.Api.Controllers;

[ApiController]
[Route("admin")]
[Authorize]
public class AdminController : ControllerBase
{
    private readonly AppDbContext _db;
    public AdminController(AppDbContext db) => _db = db;

    private async Task<bool> IsAdmin()
    {
        var email = User.FindFirstValue(ClaimTypes.Email) ?? User.FindFirstValue("email");
        if (email is null) return false;

        var u = await _db.Users.AsNoTracking().FirstOrDefaultAsync(x => x.Email == email);
        return u != null && string.Equals(u.Role, "admin", StringComparison.OrdinalIgnoreCase);
    }

    // GET /admin/users
    [HttpGet("users")]
    public async Task<IActionResult> GetUsers()
    {
        if (!await IsAdmin()) return Forbid();

        var users = await _db.Users.AsNoTracking()
            .OrderByDescending(x => x.CreatedAtUtc)
            .Select(x => new
            {
                id = x.Id,
                email = x.Email,
                firstName = x.FirstName,
                lastName = x.LastName,
                role = x.Role,
                isEnabled = x.IsEnabled,
                createdAtUtc = x.CreatedAtUtc
            })
            .ToListAsync();

        return Ok(users);
    }

    public record SetEnabledRequest(bool IsEnabled);

    [HttpPatch("users/{id:guid}/enabled")]
    public async Task<IActionResult> SetEnabled(Guid id, [FromBody] SetEnabledRequest req)
    {
        if (!await IsAdmin()) return Forbid();

        var user = await _db.Users.FirstOrDefaultAsync(x => x.Id == id);
        if (user is null) return NotFound(new { message = "User not found" });

        var myEmail = User.FindFirstValue(ClaimTypes.Email) ?? User.FindFirstValue("email");
        if (!string.IsNullOrEmpty(myEmail) && user.Email == myEmail && req.IsEnabled == false)
            return BadRequest(new { message = "You cannot disable yourself" });

        user.IsEnabled = req.IsEnabled;
        await _db.SaveChangesAsync();

        return Ok(new { ok = true, id = user.Id, isEnabled = user.IsEnabled });
    }
}
