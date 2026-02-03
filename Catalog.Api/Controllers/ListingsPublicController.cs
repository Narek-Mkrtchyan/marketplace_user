using Catalog.Api.Data;
using Catalog.Api.Dtos;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Catalog.Api.Controllers;

[ApiController]
[Route("api/listings")]
public class ListingsPublicController : ControllerBase
{
    private readonly CatalogDbContext _db;
    public ListingsPublicController(CatalogDbContext db) => _db = db;

    // GET /api/listings/home?lang=ru&take=24
    // + optional: &categoryId=...
    [HttpGet("home")]
    public async Task<ActionResult<List<ListingCardDto>>> Home(
        [FromQuery] string? lang,
        [FromQuery] int take = 24,
        [FromQuery] Guid? categoryId = null,
        CancellationToken ct = default)
    {
        lang = NormalizeLang(lang);
        take = Math.Clamp(take, 1, 60);

        var q = _db.Listings
            .AsNoTracking()
            .Where(x => x.IsPublished);

        if (categoryId.HasValue)
            q = q.Where(x => x.CategoryId == categoryId.Value);

        var items = await q
            .OrderByDescending(x => x.CreatedAtUtc)
            .Take(take)
            .Select(x => new ListingCardDto(
                x.Id,
                x.CategoryId,
                x.Title,
                x.Price,
                x.CityId,
                x.CityId == null
                    ? null
                    : x.City!.I18n.Where(i => i.Lang == lang).Select(i => i.Name).FirstOrDefault()
                      ?? x.City!.I18n.Where(i => i.Lang == "ru").Select(i => i.Name).FirstOrDefault(),
                x.Photos
                    .OrderByDescending(p => p.IsMain)
                    .ThenBy(p => p.SortOrder)
                    .Select(p => p.Url)
                    .FirstOrDefault(),
                x.CreatedAtUtc
            ))
            .ToListAsync(ct);

        return Ok(items);
    }

    private static string NormalizeLang(string? lang)
    {
        var l = (lang ?? "ru").Trim().ToLowerInvariant();
        if (l.Length > 2) l = l[..2];
        return l is "ru" or "hy" or "en" ? l : "ru";
    }
}
