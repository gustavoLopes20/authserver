using System.Collections.Generic;
using System.Threading.Tasks;

namespace AuthServer.Services.Interfaces;

public interface IEmailService
{
    Task SendEmailAsync(List<string> toEmail, string subject, string htmlContent);
}
