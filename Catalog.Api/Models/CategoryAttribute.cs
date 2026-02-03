namespace Catalog.Api.Models;

public class CategoryAttribute
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid CategoryId { get; set; }
    public Category Category { get; set; } = default!;

    public string Code { get; set; } = default!;
    public AttributeValueType Type { get; set; }

    public bool IsRequired { get; set; }
    public int SortOrder { get; set; }
    public string? Unit { get; set; }

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;

    public List<CategoryAttributeI18n> I18n { get; set; } = [];
    public List<AttributeOption> Options { get; set; } = [];
}