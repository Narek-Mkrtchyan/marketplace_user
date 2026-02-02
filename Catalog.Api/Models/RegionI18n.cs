namespace Catalog.Api.Models;

public class RegionI18n
{
    public Guid RegionId { get; set; }
    public Region Region { get; set; } = default!;

    public string Lang { get; set; } = default!;  // "hy" | "ru" | "en" | "hi"
    public string Name { get; set; } = default!;
}