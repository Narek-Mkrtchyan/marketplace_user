namespace Catalog.Api.Models;

public class City
{
    public Guid Id { get; set; }
    public Guid RegionId { get; set; }
    public Region Region { get; set; } = default!;

    public string Code { get; set; } = default!;
    public bool IsActive { get; set; } = true;

    public ICollection<CityI18n> I18n { get; set; } = new List<CityI18n>();
}