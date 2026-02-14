namespace ListamCompetitor.Api.Dtos;

public sealed record ReviewDto(
    Guid Id,
    Guid AuthorId,
    Guid TargetUserId,
    int Rating,
    string? Comment,
    DateTimeOffset CreatedAtUtc
);