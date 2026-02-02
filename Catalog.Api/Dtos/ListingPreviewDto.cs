namespace Catalog.Api.Dtos;

public record ListingPreviewDto(
    Guid Id,
    string Title,
    decimal Price,
    DateTime CreatedAtUtc,
    Guid? CityId,
    string? CityName,
    string? RegionName,
    string? MainPhotoUrl
);