namespace Catalog.Api.Dtos;

public record CategoryListItemDto(Guid Id, string Name, string Slug, Guid? ParentId, bool IsEnabled);

public record CreateCategoryRequest(string Name, string? Slug, Guid? ParentId, bool? IsEnabled);
public record UpdateCategoryRequest(string Name, string? Slug, Guid? ParentId, bool? IsEnabled);