using System;
using System.Net;
using System.Net.Mail;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;

public interface IEmailService
{
    Task SendEmailAsync(string toEmail, string subject, string body);
}

public class EmailService : IEmailService
{
    private readonly IConfiguration _config;

    public EmailService(IConfiguration config)
    {
        _config = config;
    }

    public async Task SendEmailAsync(string toEmail, string subject, string body)
    {
        var smtpServer = _config["EmailSettings:SmtpServer"];
        var smtpPort = int.Parse(_config["EmailSettings:SmtpPort"] ?? "587");
        var senderEmail = _config["EmailSettings:SenderEmail"];
        var senderName = _config["EmailSettings:SenderName"] ?? "MyCourtBooking";
        var password = _config["EmailSettings:Password"];

        // KIỂM TRA BIẾN BỊ NULL
        if (string.IsNullOrEmpty(senderEmail))
        {
            throw new Exception("Lỗi: 'SenderEmail' trong appsettings.json đang bị NULL hoặc chưa Restart app!");
        }

        if (string.IsNullOrEmpty(toEmail))
        {
            throw new Exception("Lỗi: Email người nhận (toEmail) bị NULL! Kiểm tra lại name='Email' ở Form View.");
        }

        using (var message = new MailMessage())
        {
            message.From = new MailAddress(senderEmail, senderName);
            message.To.Add(new MailAddress(toEmail));
            message.Subject = subject;
            message.Body = body;
            message.IsBodyHtml = true;

            using (var client = new SmtpClient(smtpServer, smtpPort))
            {
                client.Credentials = new NetworkCredential(senderEmail, password);
                client.EnableSsl = true;
                await client.SendMailAsync(message);
            }
        }
    }
}