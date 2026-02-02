using System.Net.Http.Json;
using Catalog.Api.Data;
using Catalog.Api.Dtos;
using Catalog.Api.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Catalog.Api.Controllers;

[ApiController]
[Route("api/listings")]
public class ListingsController : ControllerBase
{
    private readonly CatalogDbContext _db;
    private readonly IHttpClientFactory _http;

    public ListingsController(CatalogDbContext db, IHttpClientFactory http)
    {
        _db = db;
        _http = http;
    }

    private static Guid GetUserIdOrThrow(HttpRequest req)
    {
        if (!req.Headers.TryGetValue("X-User-Id", out var v) || string.IsNullOrWhiteSpace(v))
            throw new ArgumentException("Missing X-User-Id header");

        if (!Guid.TryParse(v.ToString(), out var userId))
            throw new ArgumentException("X-User-Id must be GUID");

        return userId;
    }

    private static string NormalizeLang(string? lang)
    {
        lang = (lang ?? "ru").Trim().ToLowerInvariant();
        return lang is "hy" or "ru" or "en" or "hi" ? lang : "ru";
    }

    private async Task<SellerDto?> GetSellerPublic(Guid userId, CancellationToken ct)
    {
        try
        {
            var client = _http.CreateClient("UsersApi");

            // GET /api/users/{id}/public
            var resp = await client.GetAsync($"api/users/{userId}/public", ct);
            if (!resp.IsSuccessStatusCode) return null;

            return await resp.Content.ReadFromJsonAsync<SellerDto>(cancellationToken: ct);
        }
        catch
        {
            return null;
        }
    }

    // ✅ GET /api/listings?lang=ru
    [AllowAnonymous]
    [HttpGet]
    public async Task<ActionResult<List<ListingPreviewDto>>> List([FromQuery] string? lang)
    {
        var L = NormalizeLang(lang);

        var result = await _db.Listings.AsNoTracking()
            .Where(x => x.IsPublished)
            .OrderByDescending(x => x.CreatedAtUtc)
            .Select(x => new ListingPreviewDto(
                x.Id,
                x.Title,
                x.Price,
                x.CreatedAtUtc,
                x.CityId,

                x.CityId == null ? null :
                    _db.CityI18n
                        .Where(ci => ci.CityId == x.CityId && ci.Lang == L)
                        .Select(ci => ci.Name)
                        .FirstOrDefault(),

                x.CityId == null ? null :
                    (from c in _db.Cities
                        join r in _db.Regions on c.RegionId equals r.Id
                        join ri in _db.RegionI18n on r.Id equals ri.RegionId
                        where c.Id == x.CityId && ri.Lang == L
                        select ri.Name).FirstOrDefault(),

                _db.ListingPhotos
                    .Where(p => p.ListingId == x.Id)
                    .OrderByDescending(p => p.IsMain)
                    .ThenBy(p => p.SortOrder)
                    .Select(p => p.Url)
                    .FirstOrDefault()
            ))
            .Take(50)
            .ToListAsync();

        return Ok(result);
    }

    // ✅ GET /api/listings/{id}?lang=ru
    [AllowAnonymous]
    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ListingDto>> GetOne(Guid id, [FromQuery] string? lang, CancellationToken ct)
    {
        var dto = await ToDto(id, lang, ct);
        return dto is null ? NotFound() : Ok(dto);
    }

    // ✅ POST /api/listings?lang=ru
    [HttpPost]
    public async Task<ActionResult<ListingDto>> Create([FromQuery] string? lang, [FromBody] CreateListingRequest req, CancellationToken ct)
    {
        Guid userId;
        try { userId = GetUserIdOrThrow(Request); }
        catch (Exception ex) { return BadRequest(ex.Message); }

        if (string.IsNullOrWhiteSpace(req.Title)) return BadRequest("Title is required");
        if (req.Price < 0) return BadRequest("Price must be >= 0");

        var categoryExists = await _db.Categories.AsNoTracking()
            .AnyAsync(c => c.Id == req.CategoryId && c.IsEnabled, ct);

        if (!categoryExists) return BadRequest("Category not found");

        if (req.CityId.HasValue)
        {
            var cityExists = await _db.Cities.AsNoTracking()
                .AnyAsync(c => c.Id == req.CityId.Value && c.IsActive, ct);

            if (!cityExists) return BadRequest("City not found");
        }

        var entity = new Listing
        {
            Id = Guid.NewGuid(),
            OwnerUserId = userId,
            CategoryId = req.CategoryId,
            Title = req.Title.Trim(),
            Price = req.Price,
            Description = req.Description ?? "",
            CityId = req.CityId,
            IsPublished = true,
            CreatedAtUtc = DateTime.UtcNow,
            UpdatedAtUtc = DateTime.UtcNow
        };

        _db.Listings.Add(entity);
        await _db.SaveChangesAsync(ct);

        var dto = await ToDto(entity.Id, lang, ct);
        return Ok(dto);
    }

    private async Task<ListingDto?> ToDto(Guid id, string? lang, CancellationToken ct)
    {
        var L = NormalizeLang(lang);

        var listing = await _db.Listings.AsNoTracking()
            .Include(x => x.Photos)
            .Include(x => x.City)!.ThenInclude(c => c!.Region)!.ThenInclude(r => r.I18n)
            .Include(x => x.City)!.ThenInclude(c => c!.I18n)
            .FirstOrDefaultAsync(x => x.Id == id, ct);

        if (listing is null) return null;

        string? cityName = listing.City?.I18n.FirstOrDefault(i => i.Lang == L)?.Name;
        var region = listing.City?.Region;
        string? regionName = region?.I18n.FirstOrDefault(i => i.Lang == L)?.Name;

        var photos = listing.Photos
            .OrderByDescending(p => p.IsMain)
            .ThenBy(p => p.SortOrder)
            .Select(p => new ListingPhotoDto(p.Id, p.Url, p.IsMain, p.SortOrder))
            .ToList();

        var seller = await GetSellerPublic(listing.OwnerUserId, ct);

        return new ListingDto(
            listing.Id,
            listing.OwnerUserId,
            listing.Title,
            listing.Price,
            listing.Description,
            listing.CityId,
            cityName,
            region?.Id,
            regionName,
            photos,
            seller
        );
    }
}
