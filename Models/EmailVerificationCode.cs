namespace ListamCompetitor.Api.Models;

public class EmailVerificationCode
{
    public Guid Id { get; set; }

    public string Email { get; set; } = null!;
    public string Code { get; set; } = null!;

    public DateTime ExpiresAt { get; set; }
    public bool Used { get; set; }
    public DateTime CreatedAt { get; set; }
}