using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Configuration;
using MimeKit;
using SEAL_Application.Interfaces;
using System;
using System.IO;
using System.Threading.Tasks;

namespace SEAL_Infrastructure.Services
{
    public class EmailService : IEmailService
    {
        private readonly IConfiguration _config;

        public EmailService(IConfiguration config)
        {
            _config = config;
        }

        public async Task SendEmailAsync(string toEmail, string subject, string body)
        {
            var host = _config["EmailSettings:Server"];
            var outboxPath = _config["EmailSettings:OutboxPath"];

            // DEV FALLBACK: khi CHƯA cấu hình SMTP (EmailSettings:Server rỗng) NHƯNG có EmailSettings:OutboxPath
            // (thường chỉ ở Development) -> ghi email ra file HTML để xem trước, không gửi thật.
            // Prod không cấu hình OutboxPath nên luôn đi xuống nhánh gửi SMTP bên dưới.
            if (string.IsNullOrWhiteSpace(host) && !string.IsNullOrWhiteSpace(outboxPath))
            {
                Directory.CreateDirectory(outboxPath);
                var html = $"<!-- To: {toEmail} | Subject: {subject} | {DateTime.Now:O} -->\n{body}";
                var stamp = DateTime.Now.ToString("yyyyMMdd_HHmmss_fff");
                await File.WriteAllTextAsync(Path.Combine(outboxPath, $"{stamp}.html"), html);
                await File.WriteAllTextAsync(Path.Combine(outboxPath, "latest.html"), html);
                return;
            }

            if (string.IsNullOrWhiteSpace(host))
            {
                throw new InvalidOperationException("Chưa cấu hình EmailSettings:Server.");
            }

            var senderEmail = _config["EmailSettings:SenderEmail"]
                ?? throw new InvalidOperationException("Chưa cấu hình EmailSettings:SenderEmail.");
            var username = _config["EmailSettings:Username"]
                ?? throw new InvalidOperationException("Chưa cấu hình EmailSettings:Username.");
            var password = _config["EmailSettings:Password"]
                ?? throw new InvalidOperationException("Chưa cấu hình EmailSettings:Password.");

            var email = new MimeMessage();
            email.From.Add(MailboxAddress.Parse(senderEmail));
            email.To.Add(MailboxAddress.Parse(toEmail));
            email.Subject = subject;

            var builder = new BodyBuilder { HtmlBody = body };
            email.Body = builder.ToMessageBody();

            using var smtp = new SmtpClient();
            var port = int.Parse(_config["EmailSettings:Port"] ?? "587");

            await smtp.ConnectAsync(host, port, SecureSocketOptions.StartTls);
            await smtp.AuthenticateAsync(username, password);
            await smtp.SendAsync(email);
            await smtp.DisconnectAsync(true);
        }
    }
}
