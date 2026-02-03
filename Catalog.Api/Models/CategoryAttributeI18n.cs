namespace Catalog.Api.Models;

public class CategoryAttributeI18n
{
    public Guid AttributeId { get; set; }
    public CategoryAttribute Attribute { get; set; } = default!;

    public string Lang { get; set; } = default!;
    public string Title { get; set; } = default!;
}