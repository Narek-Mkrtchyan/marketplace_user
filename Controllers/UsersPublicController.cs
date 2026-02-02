using ListamCompetitor.Api.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ListamCompetitor.Api.Controllers;

[ApiController]
[AllowAnonymous]
[Route("api/users")]
public class UsersPublicController : ControllerBase
{
    private readonly AppDbContext _db;

    public UsersPublicController(AppDbContext db) => _db = db;

    // GET /api/users/{id}/public
    [HttpGet("{id:guid}/public")]
    public async Task<IActionResult> GetPublic(Guid id)
    {
        var user = await _db.Users.AsNoTracking()
            .Where(x => x.Id == id)
            .Select(x => new
            {
                firstName = x.FirstName,
                lastName = x.LastName,
                phone = x.Phone,
                photoUrl = x.PhotoUrl
            })
            .FirstOrDefaultAsync();

        if (user is null) return NotFound();

        // ✅ displayName
        var displayName = $"{user.firstName} {user.lastName}".Trim();
        if (string.IsNullOrWhiteSpace(displayName))
            displayName = "User";

        // ✅ photoUrl absolute
        string? photoUrl = null;
        if (!string.IsNullOrWhiteSpace(user.photoUrl))
        {
            if (user.photoUrl.StartsWith("http", StringComparison.OrdinalIgnoreCase))
                photoUrl = user.photoUrl;
            else
                photoUrl = $"{Request.Scheme}://{Request.Host}{user.photoUrl}";
        }

        return Ok(new
        {
            displayName,
            avatarUrl = photoUrl,  
            phone = user.phone
        });
    }
}