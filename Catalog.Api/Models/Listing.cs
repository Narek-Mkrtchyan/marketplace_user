namespace Catalog.Api.Models;

public class Listing
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid OwnerUserId { get; set; }

    public Guid CategoryId { get; set; }           
    public Category? Category { get; set; }       

    public Guid? CityId { get; set; }
    public City? City { get; set; }

    public string Title { get; set; } = default!;
    public decimal Price { get; set; }
    public string? Description { get; set; }

    public bool IsPublished { get; set; } = true;   

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;

    public ICollection<ListingPhoto> Photos { get; set; } = new List<ListingPhoto>();
}