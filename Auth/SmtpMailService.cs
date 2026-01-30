using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Options;
using MimeKit;

namespace ListamCompetitor.Api.Auth;

public class SmtpMailService : IMailService
{
    private readonly MailSettings _s;

    public SmtpMailService(IOptions<MailSettings> opt) => _s = opt.Value;

    public async Task SendAsync(string to, string subject, string html)
    {
        var msg = new MimeMessage();
        msg.From.Add(new MailboxAddress(_s.FromName, _s.FromEmail));
        msg.To.Add(MailboxAddress.Parse(to));
        msg.Subject = subject;
        msg.Body = new BodyBuilder { HtmlBody = html }.ToMessageBody();

        using var client = new SmtpClient();

        var socketOpt = _s.UseSsl
            ? SecureSocketOptions.SslOnConnect
            : (_s.UseStartTls ? SecureSocketOptions.StartTls : SecureSocketOptions.None);

        await client.ConnectAsync(_s.Host, _s.Port, socketOpt);
        await client.AuthenticateAsync(_s.User, _s.Password);
        await client.SendAsync(msg);
        await client.DisconnectAsync(true);
    }
}