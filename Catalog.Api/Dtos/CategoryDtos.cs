namespace Catalog.Api.Dtos;

public record CategoryListItemDto(Guid Id, string Slug, Guid? ParentId, bool IsEnabled);

public record CreateCategoryRequest(string? Slug, Guid? ParentId, bool? IsEnabled);
public record UpdateCategoryRequest(string? Slug, Guid? ParentId, bool? IsEnabled);