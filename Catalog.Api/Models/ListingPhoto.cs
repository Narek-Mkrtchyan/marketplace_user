namespace Catalog.Api.Models;

public class ListingPhoto
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid ListingId { get; set; }
    public Listing Listing { get; set; } = default!;

    public string Url { get; set; } = default!;
    public int SortOrder { get; set; } = 0;
    public bool IsMain { get; set; } = false;

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
}