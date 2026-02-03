namespace Catalog.Api.Models;

public class CategoryTranslation
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid CategoryId { get; set; }
    public Category Category { get; set; } = default!;

    public string Lang { get; set; } = default!; // ru, hy, en
    public string Name { get; set; } = default!;
}