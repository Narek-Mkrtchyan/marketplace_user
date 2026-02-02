namespace Catalog.Api.Models;

public class Region
{
    public Guid Id { get; set; }
    public string Code { get; set; } = default!;
    public bool IsActive { get; set; } = true;

    public ICollection<RegionI18n> I18n { get; set; } = new List<RegionI18n>();
    public ICollection<City> Cities { get; set; } = new List<City>();
}