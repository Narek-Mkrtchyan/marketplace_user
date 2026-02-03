namespace Catalog.Api.Models;

public class ListingAttributeValue
{
    public Guid ListingId { get; set; }
    public Listing Listing { get; set; } = default!;

    public Guid AttributeId { get; set; }
    public CategoryAttribute Attribute { get; set; } = default!;

    public Guid? OptionId { get; set; }
    public AttributeOption? Option { get; set; }

    public decimal? ValueNumber { get; set; }
    public string? ValueText { get; set; }
    public bool? ValueBool { get; set; }
}