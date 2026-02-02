namespace Catalog.Api.Dtos;

public record SellerDto(
    Guid Id,
    string DisplayName,
    string? AvatarUrl,
    string? Phone
);