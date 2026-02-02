namespace Catalog.Api.Dtos;

public record RegionDto(Guid Id, string Code, string Name);
public record CityDto(Guid Id, Guid RegionId, string Code, string Name);

public record CitySearchItemDto(
    Guid Id,
    string Name,
    Guid RegionId,
    string RegionName
);