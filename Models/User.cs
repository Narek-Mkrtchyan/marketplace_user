namespace ListamCompetitor.Api.Models;

public class User
{
    public int Id { get; set; }

    public string Email { get; set; } = null!;
    public string PasswordHash { get; set; } = null!;
    
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public string? Phone { get; set; }
    public string? Gender { get; set; }   // "man" | "woman"
    public string? Country { get; set; }
    public string? City { get; set; }

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
}

