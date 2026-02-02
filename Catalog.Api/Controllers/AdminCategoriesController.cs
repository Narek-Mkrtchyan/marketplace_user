using Catalog.Api.Data;
using Catalog.Api.Dtos;
using Catalog.Api.Models;
using Catalog.Api.Utils;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Catalog.Api.Controllers;

[ApiController]
[Route("api/admin/categories")]
// [Authorize(Policy = "AdminOnly")]
public class AdminCategoriesController : ControllerBase
{
    private readonly CatalogDbContext _db;
    public AdminCategoriesController(CatalogDbContext db) => _db = db;

    [HttpGet]
    public async Task<ActionResult<List<CategoryListItemDto>>> GetAll()
    {
        var items = await _db.Categories
            .AsNoTracking()
            .OrderByDescending(x => x.CreatedAtUtc)
            .Select(x => new CategoryListItemDto(x.Id, x.Name, x.Slug, x.ParentId, x.IsEnabled))
            .ToListAsync();

        return Ok(items);
    }

    [HttpPost]
    public async Task<ActionResult<CategoryListItemDto>> Create([FromBody] CreateCategoryRequest req)
    {
        var name = (req.Name ?? "").Trim();
        if (name.Length < 2) return BadRequest("Name is too short");

        var slug = (req.Slug ?? "").Trim();
        slug = string.IsNullOrWhiteSpace(slug) ? Slug.Slugify(name) : Slug.Slugify(slug);
        if (string.IsNullOrWhiteSpace(slug)) return BadRequest("Slug is invalid");

        var exists = await _db.Categories.AnyAsync(x => x.Slug == slug);
        if (exists) return Conflict("Slug already exists");

        var c = new Category
        {
            Name = name,
            Slug = slug,
            ParentId = req.ParentId,
            IsEnabled = req.IsEnabled ?? true,
            CreatedAtUtc = DateTime.UtcNow,
            UpdatedAtUtc = DateTime.UtcNow
        };

        _db.Categories.Add(c);
        await _db.SaveChangesAsync();

        return Ok(new CategoryListItemDto(c.Id, c.Name, c.Slug, c.ParentId, c.IsEnabled));
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<CategoryListItemDto>> Update(Guid id, [FromBody] UpdateCategoryRequest req)
    {
        var c = await _db.Categories.FirstOrDefaultAsync(x => x.Id == id);
        if (c == null) return NotFound();

        var name = (req.Name ?? "").Trim();
        if (name.Length < 2) return BadRequest("Name is too short");

        var slug = (req.Slug ?? "").Trim();
        slug = string.IsNullOrWhiteSpace(slug) ? Slug.Slugify(name) : Slug.Slugify(slug);
        if (string.IsNullOrWhiteSpace(slug)) return BadRequest("Slug is invalid");

        var exists = await _db.Categories.AnyAsync(x => x.Slug == slug && x.Id != id);
        if (exists) return Conflict("Slug already exists");

        c.Name = name;
        c.Slug = slug;
        c.ParentId = req.ParentId;
        c.IsEnabled = req.IsEnabled ?? c.IsEnabled;
        c.UpdatedAtUtc = DateTime.UtcNow;

        await _db.SaveChangesAsync();

        return Ok(new CategoryListItemDto(c.Id, c.Name, c.Slug, c.ParentId, c.IsEnabled));
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var c = await _db.Categories.FirstOrDefaultAsync(x => x.Id == id);
        if (c == null) return NotFound();

        c.IsEnabled = false;
        c.UpdatedAtUtc = DateTime.UtcNow;

        await _db.SaveChangesAsync();
        return NoContent();
    }
}
