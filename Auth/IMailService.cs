namespace ListamCompetitor.Api.Auth;

public interface IMailService
{
    Task SendAsync(string to, string subject, string html);
}