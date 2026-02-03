namespace Catalog.Api.Dtos;

public sealed record ListingCardDto(
    Guid Id,
    Guid CategoryId,
    string Title,
    decimal Price,
    Guid? CityId,
    string? CityName,
    string? MainPhotoUrl,
    DateTime CreatedAtUtc
);