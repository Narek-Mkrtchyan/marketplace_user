namespace Catalog.Api.Dtos;

public record CreateListingRequest(
    string Title,
    decimal Price,
    string? Description,
    Guid? CityId,
    Guid CategoryId
    
);

public record ListingPhotoDto(Guid Id, string Url, bool IsMain, int SortOrder);

public record ListingDto(
    Guid Id,
    Guid UserId,
    string Title,
    decimal Price,
    string? Description,
    Guid? CityId,
    string? CityName,
    Guid? RegionId,
    string? RegionName,
    List<ListingPhotoDto> Photos,
    SellerDto? Seller
);