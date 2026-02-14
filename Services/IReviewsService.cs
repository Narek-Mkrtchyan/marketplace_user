using ListamCompetitor.Api.Dtos;

namespace ListamCompetitor.Api.Services;

public interface IReviewsService
{
    Task<ReviewDto> CreateAsync(Guid authorId, CreateReviewRequest request, CancellationToken ct);
    Task<UserRatingDto> GetUserRatingAsync(Guid userId, CancellationToken ct);
    Task<List<ReviewDto>> GetUserReviewsAsync(Guid userId, CancellationToken ct);
}
