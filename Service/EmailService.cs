using System.Net;
using System.Net.Mail;
namespace Floaty_Music.Service
{
    public class EmailService
    {
        public async Task SendEmailAsync(string to, string subject, string body)
        {
            var smtp = new SmtpClient(GlobalConfiguration.SMTP_SERVER)
            {
                Port = int.TryParse(GlobalConfiguration.SMTP_PORT, out var p) ? p : 587,
                Credentials = new NetworkCredential(GlobalConfiguration.SMTP_EMAIL, GlobalConfiguration.SMTP_PASSWORD),
                EnableSsl = true
            };

            var mail = new MailMessage(GlobalConfiguration.SMTP_EMAIL, to, subject, body)
            {
                IsBodyHtml = true
            };

            await smtp.SendMailAsync(mail);
        }
    }

}
