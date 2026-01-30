namespace ListamCompetitor.Api.Contracts.Profile;

public record UpdateProfileRequest(
    string? FirstName,
    string? LastName,
    string? Phone,
    string? Gender,
    string? Country,
    string? City
);