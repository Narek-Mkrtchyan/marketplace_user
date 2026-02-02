using Catalog.Api.Data;
using Catalog.Api.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Catalog.Api.Controllers;

[ApiController]
[Route("api/listings/{listingId:guid}/photos")]
public class ListingPhotosController : ControllerBase
{
    private readonly CatalogDbContext _db;
    private readonly IWebHostEnvironment _env;

    public ListingPhotosController(CatalogDbContext db, IWebHostEnvironment env)
    {
        _db = db;
        _env = env;
    }

    private static Guid GetUserIdOrThrow(HttpRequest req)
    {
        if (!req.Headers.TryGetValue("X-User-Id", out var v) || string.IsNullOrWhiteSpace(v))
            throw new ArgumentException("Missing X-User-Id header");

        if (!Guid.TryParse(v.ToString(), out var userId))
            throw new ArgumentException("X-User-Id must be GUID");

        return userId;
    }

    // POST /api/listings/{id}/photos  (multipart)
    [HttpPost]
    [RequestSizeLimit(20_000_000)] // 20MB
    public async Task<IActionResult> Upload(Guid listingId, [FromForm] IFormFile file, [FromForm] bool? isMain)
    {
        Guid userId;
        try { userId = GetUserIdOrThrow(Request); }
        catch (Exception ex) { return BadRequest(ex.Message); }

        var listing = await _db.Listings.FirstOrDefaultAsync(x => x.Id == listingId);
        if (listing is null) return NotFound("Listing not found");
        if (listing.OwnerUserId != userId) return Forbid();

        if (file is null || file.Length == 0) return BadRequest("file is required");
        if (!file.ContentType.StartsWith("image/")) return BadRequest("Only images allowed");

        var uploadsDir = Path.Combine(_env.ContentRootPath, "wwwroot", "uploads", "listings", listingId.ToString());
        Directory.CreateDirectory(uploadsDir);

        var ext = Path.GetExtension(file.FileName);
        if (string.IsNullOrWhiteSpace(ext)) ext = ".jpg";

        var photoId = Guid.NewGuid();
        var fileName = $"{photoId}{ext}";
        var fullPath = Path.Combine(uploadsDir, fileName);

        await using (var fs = System.IO.File.Create(fullPath))
            await file.CopyToAsync(fs);

        var url = $"/uploads/listings/{listingId}/{fileName}";

        var hasAny = await _db.ListingPhotos.AnyAsync(p => p.ListingId == listingId);
        var makeMain = isMain == true || !hasAny;

        if (makeMain)
        {
            await _db.Database.ExecuteSqlInterpolatedAsync(
                $"UPDATE public.listing_photos SET is_main = false WHERE listing_id = {listingId}");
        }

        var entity = new ListingPhoto
        {
            Id = photoId,
            ListingId = listingId,
            Url = url,
            IsMain = makeMain,
            SortOrder = 0
        };

        _db.ListingPhotos.Add(entity);
        await _db.SaveChangesAsync();

        return Ok(new { id = entity.Id, url = entity.Url, isMain = entity.IsMain });
    }

    // PUT /api/listings/{id}/photos/{photoId}/main
    [HttpPut("{photoId:guid}/main")]
    public async Task<IActionResult> SetMain(Guid listingId, Guid photoId)
    {
        Guid userId;
        try { userId = GetUserIdOrThrow(Request); }
        catch (Exception ex) { return BadRequest(ex.Message); }

        var listing = await _db.Listings.AsNoTracking().FirstOrDefaultAsync(x => x.Id == listingId);
        if (listing is null) return NotFound("Listing not found");
        if (listing.OwnerUserId != userId) return Forbid();

        var exists = await _db.ListingPhotos.AnyAsync(p => p.Id == photoId && p.ListingId == listingId);
        if (!exists) return NotFound("Photo not found");

        await _db.Database.ExecuteSqlInterpolatedAsync(
            $"UPDATE public.listing_photos SET is_main = false WHERE listing_id = {listingId}");
        await _db.Database.ExecuteSqlInterpolatedAsync(
            $"UPDATE public.listing_photos SET is_main = true WHERE id = {photoId} AND listing_id = {listingId}");

        return Ok(new { ok = true });
    }
}
