namespace ListamCompetitor.Api.Dtos;

public sealed record UserRatingDto(
    double AverageRating,
    int TotalReviews
);