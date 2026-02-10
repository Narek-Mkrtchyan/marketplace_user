using Microsoft.AspNetCore.Mvc;

namespace ListamCompetitor.Api.Controllers;

[ApiController]
[Route("health")]
public class HealthController : ControllerBase
{
    [HttpGet("debug/claims")]
    public IActionResult Claims()
    {
        return Ok(User.Claims.Select(c => new { c.Type, c.Value }).ToList());
    }

}
