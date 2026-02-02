namespace Catalog.Api.Models;

public class CityI18n
{
    public Guid CityId { get; set; }
    public City City { get; set; } = default!;

    public string Lang { get; set; } = default!;
    public string Name { get; set; } = default!;
}