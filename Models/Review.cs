namespace ListamCompetitor.Api.Models;

public sealed class Review
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid AuthorId { get; set; }
    public Guid TargetUserId { get; set; }

    public int Rating { get; set; }
    public string? Comment { get; set; }

    public DateTimeOffset CreatedAtUtc { get; set; } = DateTimeOffset.UtcNow;
}