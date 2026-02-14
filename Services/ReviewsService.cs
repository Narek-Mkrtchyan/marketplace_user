using ListamCompetitor.Api.Data;
using ListamCompetitor.Api.Dtos;
using ListamCompetitor.Api.Models;

namespace ListamCompetitor.Api.Services;
using Microsoft.EntityFrameworkCore;

public sealed class ReviewsService : IReviewsService
{
    private readonly AppDbContext _db;

    public ReviewsService(AppDbContext db)
    {
        _db = db;
    }

    public async Task<ReviewDto> CreateAsync(Guid authorId, CreateReviewRequest request, CancellationToken ct)
    {
        if (authorId == request.TargetUserId)
            throw new ArgumentException("Cannot review yourself");

        if (request.Rating < 1 || request.Rating > 5)
            throw new ArgumentException("Rating must be between 1 and 5");

        var review = new Review()
        {
            AuthorId = authorId,
            TargetUserId = request.TargetUserId,
            Rating = request.Rating,
            Comment = request.Comment?.Trim()
        };

        _db.Reviews.Add(review);
        await _db.SaveChangesAsync(ct);

        return new ReviewDto(
            review.Id,
            review.AuthorId,
            review.TargetUserId,
            review.Rating,
            review.Comment,
            review.CreatedAtUtc
        );
    }

    public async Task<UserRatingDto> GetUserRatingAsync(Guid userId, CancellationToken ct)
    {
        var query = _db.Reviews.Where(x => x.TargetUserId == userId);

        var total = await query.CountAsync(ct);
        var avg = total == 0
            ? 0
            : await query.AverageAsync(x => x.Rating, ct);

        return new UserRatingDto(Math.Round(avg, 2), total);
    }

    public async Task<List<ReviewDto>> GetUserReviewsAsync(Guid userId, CancellationToken ct)
    {
        return await _db.Reviews
            .Where(x => x.TargetUserId == userId)
            .OrderByDescending(x => x.CreatedAtUtc)
            .Select(x => new ReviewDto(
                x.Id,
                x.AuthorId,
                x.TargetUserId,
                x.Rating,
                x.Comment,
                x.CreatedAtUtc))
            .ToListAsync(ct);
    }
}
