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

    // ----------------------------
    // GET /api/geo/regions?lang=ru
    // ----------------------------
    [HttpGet("regions")]
    public async Task<ActionResult<List<RegionDto>>> GetRegions([FromQuery] string? lang)
    {
        var L = NormalizeLang(lang);

        var items = await _db.Regions.AsNoTracking()
            .Where(r => r.IsActive)
            .Select(r => new
            {
                r.Id,
                r.Code,
                Name = _db.RegionI18n
                    .Where(i => i.RegionId == r.Id && i.Lang == L)
                    .Select(i => i.Name)
                    .FirstOrDefault() ?? r.Code
            })
            .OrderBy(x => x.Name)
            .Select(x => new RegionDto(x.Id, x.Code, x.Name))
            .ToListAsync();

        return Ok(items);
    }

    // ---------------------------------------------
    // GET /api/geo/cities?region=AM-01&lang=ru&query=
    // ---------------------------------------------
    [HttpGet("cities")]
    public async Task<ActionResult<List<CityDto>>> GetCities(
        [FromQuery] string? region,
        [FromQuery] string? lang,
        [FromQuery] string? query
    )
    {
        var L = NormalizeLang(lang);

        // 1️⃣ базовый набор: ВСЕ активные города (включая без области)
        var cities = _db.Cities.AsNoTracking()
            .Where(c => c.IsActive);

        // 2️⃣ фильтр по области — ТОЛЬКО если region передан
        if (!string.IsNullOrWhiteSpace(region))
        {
            var reg = region.Trim();

            cities =
                from c in cities
                join r in _db.Regions.AsNoTracking()
                    on c.RegionId equals r.Id   // RegionId NULL сюда не попадёт — это ок
                where r.Code == reg
                select c;
        }

        // 3️⃣ сначала считаем Name → потом сортировка
        var rows = cities.Select(c => new
        {
            c.Id,
            c.RegionId,
            c.Code,
            Name =
                _db.CityI18n
                    .Where(i => i.CityId == c.Id && i.Lang == L)
                    .Select(i => i.Name)
                    .FirstOrDefault() ?? c.Code
        });

        // 4️⃣ поиск
        if (!string.IsNullOrWhiteSpace(query))
        {
            var s = query.Trim();
            rows = rows.Where(x =>
                EF.Functions.ILike(x.Name, $"%{s}%") ||
                EF.Functions.ILike(x.Code, $"%{s}%")
            );
        }

        // 5️⃣ финал
        var items = await rows
            .OrderBy(x => x.Name)
            .Select(x => new CityDto(
                x.Id,
                x.RegionId,
                x.Code,
                x.Name
            ))
            .ToListAsync();

        return Ok(items);
    }



    // -------------------------------------
    // GET /api/geo/cities/search?lang=ru&q=va
    // -------------------------------------
    [HttpGet("cities/search")]
    public async Task<ActionResult<List<CitySearchItemDto>>> SearchCities(
        [FromQuery] string? lang,
        [FromQuery(Name = "q")] string? q
    )
    {
        var L = NormalizeLang(lang);
        var s = (q ?? "").Trim();
        if (s.Length < 2) return Ok(new List<CitySearchItemDto>());

        var baseQuery =
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
                  && (
                      EF.Functions.ILike(ci.Name, $"%{s}%")
                      || EF.Functions.ILike(c.Code, $"%{s}%") 
                  )
            select new
            {
                CityId = c.Id,
                CityName = ci.Name,
                RegionId = r.Id,
                RegionName = ri.Name
            };

        var rows = await baseQuery
            .Distinct()
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
