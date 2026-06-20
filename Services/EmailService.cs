using AuthServer.Services.Interfaces;
using Microsoft.Extensions.Configuration;
using MimeKit;
using MimeKit.Text;
using System.Threading.Tasks;
using MailKit.Net.Smtp;
using MailKit.Security;

namespace AuthServer.Services;

public class EmailService(IConfiguration configuration) : IEmailService
{
    private readonly IConfiguration _configuration = configuration;

    public async Task SendEmailAsync(string toEmail, string subject, string htmlContent)
    {
        var from = _configuration["OpenIddict:Smtp:From"];
        var host = _configuration["OpenIddict:Smtp:Host"];
        var port = _configuration["OpenIddict:Smtp:Port"];
        var username = _configuration["OpenIddict:Smtp:Username"];
        var password = _configuration["OpenIddict:Smtp:PasswordApp"];   

        var email = new MimeMessage();
        email.From.Add(MailboxAddress.Parse(from));
        email.To.Add(MailboxAddress.Parse(toEmail));
        email.Subject = subject;
        email.Body = new TextPart(TextFormat.Html) { Text = htmlContent };

        using var smtp = new SmtpClient();

        // Configuração de conexão segura
        await smtp.ConnectAsync(
            host,
            int.Parse(port),
            SecureSocketOptions.StartTls
        );

        await smtp.AuthenticateAsync(username, password);
        await smtp.SendAsync(email);
        await smtp.DisconnectAsync(true);
    }
}
