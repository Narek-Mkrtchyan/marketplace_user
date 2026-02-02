using Catalog.Api.Data;
using Catalog.Api.Dtos;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Catalog.Api.Controllers;

[ApiController]
[Route("api/geo")]
public class GeoController : ControllerBase
{
    private readonly CatalogDbContext _db;
    public GeoController(CatalogDbContext db) => _db = db;

    private static string NormalizeLang(string? lang)
    {
        lang = (lang ?? "ru").Trim().ToLowerInvariant();
        return lang is "hy" or "ru" or "en" or "hi" ? lang : "ru";
    }

    [HttpGet("regions")]
    public async Task<ActionResult<List<RegionDto>>> GetRegions([FromQuery] string? lang)
    {
        var L = NormalizeLang(lang);

        var items = await _db.Regions.AsNoTracking()
            .Where(r => r.IsActive)
            .Join(_db.RegionI18n.AsNoTracking().Where(i => i.Lang == L),
                r => r.Id,
                i => i.RegionId,
                (r, i) => new RegionDto(r.Id, r.Code, i.Name))
            .OrderBy(x => x.Name)
            .ToListAsync();

        return Ok(items);
    }

    [HttpGet("cities")]
    public async Task<ActionResult<List<CityDto>>> GetCities(
        [FromQuery] string? region,
        [FromQuery] string? lang,
        [FromQuery] string? query
    )
    {
        var L = NormalizeLang(lang);

        var q = _db.Cities.AsNoTracking()
            .Where(c => c.IsActive);

        if (!string.IsNullOrWhiteSpace(region))
        {
            q = q.Where(c => c.Region.Code == region);
        }

        if (!string.IsNullOrWhiteSpace(query))
        {
            var s = query.Trim();
            q = q.Where(c => c.I18n.Any(i => i.Lang == L && EF.Functions.ILike(i.Name, $"%{s}%")));
        }

        var items = await q
            .Select(c => new CityDto(
                c.Id,
                c.RegionId,
                c.Code,
                c.I18n.Where(i => i.Lang == L).Select(i => i.Name).FirstOrDefault() ?? c.Code
            ))
            .OrderBy(x => x.Name)
            .ToListAsync();

        return Ok(items);
    }

    [HttpGet("cities/search")]
    public async Task<ActionResult<List<CitySearchItemDto>>> SearchCities(
        [FromQuery] string? lang,
        [FromQuery(Name = "q")] string? q
    )
    {
        var L = NormalizeLang(lang);
        var s = (q ?? "").Trim();
        if (s.Length < 2) return Ok(new List<CitySearchItemDto>());

        var query =
            from c in _db.Cities.AsNoTracking()
            join ci in _db.CityI18n.AsNoTracking()
                on c.Id equals ci.CityId
            join r in _db.Regions.AsNoTracking()
                on c.RegionId equals r.Id
            join ri in _db.RegionI18n.AsNoTracking()
                on r.Id equals ri.RegionId
            where c.IsActive
                  && ci.Lang == L
                  && ri.Lang == L
                  && EF.Functions.ILike(ci.Name, $"%{s}%")
            select new
            {
                CityId = c.Id,
                CityName = ci.Name,
                RegionId = r.Id,
                RegionName = ri.Name
            };

        // 2) Сортируем по строке CityName (не по DTO)
        var rows = await query
            .OrderBy(x => x.CityName)
            .Take(20)
            .ToListAsync();

        var result = rows
            .Select(x => new CitySearchItemDto(
                x.CityId,
                x.CityName,
                x.RegionId,
                x.RegionName
            ))
            .ToList();

        return Ok(result);
    }

}
