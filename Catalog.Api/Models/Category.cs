namespace Catalog.Api.Models;

public class Category
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public string Slug { get; set; } = default!;

    public string? Icon { get; set; }         
    public int SortOrder { get; set; }

    public Guid? ParentId { get; set; }
    public Category? Parent { get; set; }
    public List<Category> Children { get; set; } = [];

    public bool IsEnabled { get; set; } = true;

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;

    public List<CategoryTranslation> Translations { get; set; } = [];
    public List<CategoryAttribute> Attributes { get; set; } = [];

}