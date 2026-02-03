using Catalog.Api.Data;
using Catalog.Api.Dtos;
using Catalog.Api.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Catalog.Api.Controllers;

public sealed record CategoryNodeDto(
    Guid Id,
    string Slug,
    string Title,
    string? Icon,
    List<CategoryNodeDto> Children
);

[ApiController]
[Route("api/categories")]
public class CategoriesController : ControllerBase
{
    private readonly CatalogDbContext _db;
    public CategoriesController(CatalogDbContext db) => _db = db;

    // GET /api/categories?lang=ru
    [HttpGet]
    public async Task<ActionResult<List<CategoryNodeDto>>> GetTree([FromQuery] string? lang, CancellationToken ct)
    {
        lang = NormalizeLang(lang);

        var cats = await _db.Categories
            .AsNoTracking()
            .Where(x => x.IsEnabled)
            .OrderBy(x => x.SortOrder)
            .Select(x => new
            {
                x.Id,
                x.Slug,
                x.Icon,
                x.ParentId,
                Title =
                    x.Translations.Where(t => t.Lang == lang).Select(t => t.Name).FirstOrDefault()
                    ?? x.Translations.Where(t => t.Lang == "ru").Select(t => t.Name).FirstOrDefault()
                    ?? x.Slug
            })
            .ToListAsync(ct);

        var map = cats.ToDictionary(
            x => x.Id,
            x => new CategoryNodeDto(x.Id, x.Slug, x.Title, x.Icon, new List<CategoryNodeDto>())
        );

        var roots = new List<CategoryNodeDto>();

        foreach (var c in cats)
        {
            if (c.ParentId is null)
                roots.Add(map[c.Id]);
            else if (map.TryGetValue(c.ParentId.Value, out var parent))
                parent.Children.Add(map[c.Id]);
        }

        return Ok(roots);
    }

    // GET /api/categories/{categoryId}/attributes?lang=ru
    [HttpGet("{categoryId:guid}/attributes")]
    public async Task<ActionResult<List<CategoryAttributeDto>>> GetAttributes(
        Guid categoryId,
        [FromQuery] string? lang,
        CancellationToken ct)
    {
        lang = NormalizeLang(lang);

        var exists = await _db.Categories.AsNoTracking()
            .AnyAsync(x => x.Id == categoryId && x.IsEnabled, ct);

        if (!exists) return NotFound();

        var data = await _db.CategoryAttributes
            .AsNoTracking()
            .Where(x => x.CategoryId == categoryId)
            .OrderBy(x => x.SortOrder)
            .Select(x => new CategoryAttributeDto(
                x.Id,
                x.Code,
                x.I18n.Where(i => i.Lang == lang).Select(i => i.Title).FirstOrDefault()
                    ?? x.I18n.Where(i => i.Lang == "ru").Select(i => i.Title).FirstOrDefault()
                    ?? x.Code,
                x.Type == AttributeValueType.Select ? "select"
                    : x.Type == AttributeValueType.Number ? "number"
                    : x.Type == AttributeValueType.Bool ? "bool"
                    : "text",
                x.IsRequired,
                x.SortOrder,
                x.Unit,
                x.Type == AttributeValueType.Select
                    ? x.Options.Where(o => o.IsActive)
                        .OrderBy(o => o.SortOrder)
                        .Select(o => new AttributeOptionDto(
                            o.Id,
                            o.Code,
                            o.I18n.Where(i => i.Lang == lang).Select(i => i.Title).FirstOrDefault()
                                ?? o.I18n.Where(i => i.Lang == "ru").Select(i => i.Title).FirstOrDefault()
                                ?? o.Code
                        ))
                        .ToList()
                    : null
            ))
            .ToListAsync(ct);

        return Ok(data);
    }

    private static string NormalizeLang(string? lang)
    {
        var l = (lang ?? "ru").Trim().ToLowerInvariant();
        if (l.Length > 2) l = l[..2];
        return l is "ru" or "hy" or "en" ? l : "ru";
    }
}
