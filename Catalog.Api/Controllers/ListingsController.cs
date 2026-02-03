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

    // ✅ POST /api/listings (JSON)
    [HttpPost]
    public async Task<ActionResult<ListingDto>> Create(
        [FromQuery] string? lang,
        [FromBody] CreateListingRequest req,
        CancellationToken ct)
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

    // ========= MULTIPART =========

    public class CreateListingForm
    {
        public Guid CategoryId { get; set; }
        public Guid? CityId { get; set; }
        public string Title { get; set; } = "";
        public decimal Price { get; set; }
        public string? Description { get; set; }

        // имя поля на фронте: photos
        public List<IFormFile> Photos { get; set; } = new();
    }

    // ✅ POST /api/listings/multipart  (FormData + photos)
    [HttpPost("multipart")]
    [Consumes("multipart/form-data")]
    public async Task<ActionResult<ListingDto>> CreateMultipart(
        [FromQuery] string? lang,
        [FromForm] CreateListingForm form,
        CancellationToken ct)
    {
        Guid userId;
        try { userId = GetUserIdOrThrow(Request); }
        catch (Exception ex) { return BadRequest(ex.Message); }

        if (string.IsNullOrWhiteSpace(form.Title)) return BadRequest("Title is required");
        if (form.Price < 0) return BadRequest("Price must be >= 0");

        var categoryExists = await _db.Categories.AsNoTracking()
            .AnyAsync(c => c.Id == form.CategoryId && c.IsEnabled, ct);
        if (!categoryExists) return BadRequest("Category not found");

        if (form.CityId.HasValue)
        {
            var cityExists = await _db.Cities.AsNoTracking()
                .AnyAsync(c => c.Id == form.CityId.Value && c.IsActive, ct);
            if (!cityExists) return BadRequest("City not found");
        }

        var entity = new Listing
        {
            Id = Guid.NewGuid(),
            OwnerUserId = userId,
            CategoryId = form.CategoryId,
            Title = form.Title.Trim(),
            Price = form.Price,
            Description = form.Description ?? "",
            CityId = form.CityId,
            IsPublished = true,
            CreatedAtUtc = DateTime.UtcNow,
            UpdatedAtUtc = DateTime.UtcNow
        };

        _db.Listings.Add(entity);
        await _db.SaveChangesAsync(ct);

        // ✅ сохранить фото
        if (form.Photos is { Count: > 0 })
        {
            var root = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");
            var dir = Path.Combine(root, "uploads", "listings", entity.Id.ToString());
            Directory.CreateDirectory(dir);

            var sortOrder = 1;
            foreach (var file in form.Photos)
            {
                if (file.Length == 0) continue;

                var ext = Path.GetExtension(file.FileName);
                if (string.IsNullOrWhiteSpace(ext)) ext = ".jpg";

                var photoId = Guid.NewGuid();
                var fileName = $"{photoId}{ext}";
                var absPath = Path.Combine(dir, fileName);

                await using (var stream = System.IO.File.Create(absPath))
                    await file.CopyToAsync(stream, ct);

                var url = $"/uploads/listings/{entity.Id}/{fileName}";

                _db.ListingPhotos.Add(new ListingPhoto
                {
                    Id = photoId,
                    ListingId = entity.Id,
                    Url = url,
                    SortOrder = sortOrder,
                    IsMain = sortOrder == 1 // первая — main
                });

                sortOrder++;
            }

            await _db.SaveChangesAsync(ct);
        }

        var dto = await ToDto(entity.Id, lang, ct);
        return Ok(dto);
    }

    // ========= DTO =========

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
    
    // ✅ GET /api/listings/my?lang=ru
   
    [HttpGet("my")]
    public async Task<ActionResult<List<ListingPreviewDto>>> My([FromQuery] string? lang, CancellationToken ct)
    {
        Guid userId;
        try { userId = GetUserIdOrThrow(Request); }
        catch (Exception ex) { return BadRequest(ex.Message); }

        var L = NormalizeLang(lang);

        var result = await _db.Listings.AsNoTracking()
            .Where(x => x.OwnerUserId == userId)          // ✅ ВАЖНО: OwnerUserId
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
            .ToListAsync(ct);

        return Ok(result);
    }

    // ✅ PUT /api/listings/{id}
    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(
        Guid id,
        [FromBody] CreateListingRequest req,
        CancellationToken ct)
    {
        Guid userId;
        try { userId = GetUserIdOrThrow(Request); }
        catch (Exception ex) { return BadRequest(ex.Message); }

        var entity = await _db.Listings
            .FirstOrDefaultAsync(x => x.Id == id, ct);

        if (entity is null)
            return NotFound();

        
        if (entity.OwnerUserId != userId)
            return Forbid();

        if (string.IsNullOrWhiteSpace(req.Title))
            return BadRequest("Title is required");

        if (req.Price < 0)
            return BadRequest("Price must be >= 0");

        entity.Title = req.Title.Trim();
        entity.Price = req.Price;
        entity.Description = req.Description ?? "";
        entity.CityId = req.CityId;
        entity.CategoryId = req.CategoryId;
        entity.UpdatedAtUtc = DateTime.UtcNow;

        await _db.SaveChangesAsync(ct);

        var dto = await ToDto(entity.Id, null, ct);
        return Ok(dto);
    }

    [HttpPost("{id:guid}/photos")]
    [Consumes("multipart/form-data")]
    public async Task<IActionResult> AddPhotos(Guid id, [FromForm] List<IFormFile> photos, CancellationToken ct)
    {
        Guid userId;
        try { userId = GetUserIdOrThrow(Request); }
        catch (Exception ex) { return BadRequest(ex.Message); }

        var listing = await _db.Listings.FirstOrDefaultAsync(x => x.Id == id, ct);
        if (listing is null) return NotFound();
        if (listing.OwnerUserId != userId) return Forbid();

        if (photos is null || photos.Count == 0) return BadRequest("No photos");

        var root = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");
        var dir = Path.Combine(root, "uploads", "listings", listing.Id.ToString());
        Directory.CreateDirectory(dir);

        var maxSort = await _db.ListingPhotos
            .Where(p => p.ListingId == listing.Id)
            .Select(p => (int?)p.SortOrder)
            .MaxAsync(ct) ?? 0;

        var sortOrder = maxSort + 1;

        foreach (var file in photos)
        {
            if (file.Length == 0) continue;

            var ext = Path.GetExtension(file.FileName);
            if (string.IsNullOrWhiteSpace(ext)) ext = ".jpg";

            var photoId = Guid.NewGuid();
            var fileName = $"{photoId}{ext}";
            var absPath = Path.Combine(dir, fileName);

            await using (var stream = System.IO.File.Create(absPath))
                await file.CopyToAsync(stream, ct);

            var url = $"/uploads/listings/{listing.Id}/{fileName}";

            _db.ListingPhotos.Add(new ListingPhoto
            {
                Id = photoId,
                ListingId = listing.Id,
                Url = url,
                SortOrder = sortOrder,
                IsMain = false
            });

            sortOrder++;
        }

        await _db.SaveChangesAsync(ct);

        var dto = await ToDto(listing.Id, null, ct);
        return Ok(dto);
    }
    
    [HttpPost("{id:guid}/photos/{photoId:guid}/main")]
    public async Task<IActionResult> SetMainPhoto(Guid id, Guid photoId, CancellationToken ct)
    {
        Guid userId;
        try { userId = GetUserIdOrThrow(Request); }
        catch (Exception ex) { return BadRequest(ex.Message); }

        var listing = await _db.Listings.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id, ct);
        if (listing is null) return NotFound();
        if (listing.OwnerUserId != userId) return Forbid();

        var ok = await _db.ListingPhotos
            .AsNoTracking()
            .AnyAsync(p => p.Id == photoId && p.ListingId == id, ct);

        if (!ok) return BadRequest("Photo does not belong to listing");

        await _db.Database.ExecuteSqlInterpolatedAsync($@"
        UPDATE public.listing_photos
        SET is_main = (id = {photoId})
        WHERE listing_id = {id};
    ", ct);

        var dto = await ToDto(id, null, ct);
        return Ok(dto);
    }



    [HttpDelete("{id:guid}/photos/{photoId:guid}")]
    public async Task<IActionResult> DeletePhoto(Guid id, Guid photoId, CancellationToken ct)
    {
        Guid userId;
        try { userId = GetUserIdOrThrow(Request); }
        catch (Exception ex) { return BadRequest(ex.Message); }

        // listing ownership check
        var listing = await _db.Listings.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id, ct);
        if (listing is null) return NotFound();
        if (listing.OwnerUserId != userId) return Forbid();

        var photo = await _db.ListingPhotos.FirstOrDefaultAsync(p => p.Id == photoId && p.ListingId == id, ct);
        if (photo is null) return NotFound();

        var wasMain = photo.IsMain;

        try
        {
            var rel = photo.Url.TrimStart('/').Replace("/", Path.DirectorySeparatorChar.ToString());
            var abs = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", rel);
            if (System.IO.File.Exists(abs)) System.IO.File.Delete(abs);
        }
        catch
        {
        }

        _db.ListingPhotos.Remove(photo);
        await _db.SaveChangesAsync(ct);

        if (wasMain)
        {
            var next = await _db.ListingPhotos
                .Where(p => p.ListingId == id)
                .OrderBy(p => p.SortOrder)
                .FirstOrDefaultAsync(ct);

            if (next != null)
            {
                next.IsMain = true;
                await _db.SaveChangesAsync(ct);
            }
        }

        var dto = await ToDto(id, null, ct);
        return Ok(dto);
    }

}
