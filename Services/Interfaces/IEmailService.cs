using System.Threading.Tasks;

namespace AuthServer.Services.Interfaces;

public interface IEmailService
{
    Task SendEmailAsync(string toEmail, string subject, string htmlContent);
}
