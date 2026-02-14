namespace ListamCompetitor.Api.Dtos;

public sealed record CreateReviewRequest(
    Guid TargetUserId,
    int Rating,
    string? Comment
);