namespace Catalog.Api.Dtos;

public sealed record CategoryTreeItemDto(
    Guid Id,
    string Title,
    string Slug,
    string? Icon,
    List<CategoryTreeItemDto> Children
);