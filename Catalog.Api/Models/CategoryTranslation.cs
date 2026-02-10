namespace Catalog.Api.Models;

public class CategoryTranslation
{
    public Guid CategoryId { get; set; }
    public Category Category { get; set; } = default!;

    public string Lang { get; set; } = default!; // ru, hy, en
    public string Title { get; set; } = default!;
}