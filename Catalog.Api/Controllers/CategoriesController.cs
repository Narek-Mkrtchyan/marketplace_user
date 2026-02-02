using Catalog.Api.Data;
using Catalog.Api.Dtos;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Catalog.Api.Controllers;

// [ApiController]
[Route("api/categories")]
public class CategoriesController : ControllerBase
{
    private readonly CatalogDbContext _db;
    public CategoriesController(CatalogDbContext db) => _db = db;

    [HttpGet]
    public async Task<ActionResult<List<CategoryListItemDto>>> GetAll()
    {
        var items = await _db.Categories
            .AsNoTracking()
            .Where(x => x.IsEnabled)
            .OrderBy(x => x.Name)
            .Select(x => new CategoryListItemDto(x.Id, x.Name, x.Slug, x.ParentId, x.IsEnabled))
            .ToListAsync();

        return Ok(items);
    }
}