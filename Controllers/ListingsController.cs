using ListamCompetitor.Api.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ListamCompetitor.Api.Controllers;

[ApiController]
[Route("listings")]
public class ListingsController : ControllerBase
{
    private readonly AppDbContext _db;

    public ListingsController(AppDbContext db) => _db = db;

    // GET /listings?q=...&city=...&minPrice=...&maxPrice=...
    [HttpGet]
    public async Task<IActionResult> GetMany([FromQuery] string? q, [FromQuery] string? city, [FromQuery] decimal? minPrice, [FromQuery] decimal? maxPrice)
    {
        var query = _db.Listings.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(q))
            query = query.Where(x => x.Title.Contains(q) || x.Description.Contains(q));

        if (!string.IsNullOrWhiteSpace(city))
            query = query.Where(x => x.City == city);

        if (minPrice is not null)
            query = query.Where(x => x.Price >= minPrice);

        if (maxPrice is not null)
            query = query.Where(x => x.Price <= maxPrice);

        var items = await query
            .OrderByDescending(x => x.CreatedAtUtc)
            .Take(50)
            .Select(x => new
            {
                x.Id,
                x.Title,
                x.Price,
                x.City,
                x.OwnerEmail,
                x.CreatedAtUtc
            })
            .ToListAsync();

        return Ok(items);
    }

    // GET /listings/123
    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetOne([FromRoute] int id)
    {
        var x = await _db.Listings.AsNoTracking().FirstOrDefaultAsync(l => l.Id == id);
        return x is null ? NotFound() : Ok(x);
    }
}