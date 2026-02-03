namespace Catalog.Api.Models;

public class AttributeOption
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid AttributeId { get; set; }
    public CategoryAttribute Attribute { get; set; } = default!;

    public string Code { get; set; } = default!;
    public int SortOrder { get; set; }
    public bool IsActive { get; set; } = true;

    public List<AttributeOptionI18n> I18n { get; set; } = [];
}

public class AttributeOptionI18n
{
    public Guid OptionId { get; set; }
    public AttributeOption Option { get; set; } = default!;

    public string Lang { get; set; } = default!;
    public string Title { get; set; } = default!;
}