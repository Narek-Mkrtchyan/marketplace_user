namespace Catalog.Api.Dtos;

public sealed record AttributeOptionDto(Guid Id, string Code, string Title);

public sealed record CategoryAttributeDto(
    Guid Id,
    string Code,
    string Title,
    string Type,          // select|number|text|bool
    bool IsRequired,
    int SortOrder,
    string? Unit,
    List<AttributeOptionDto>? Options
);