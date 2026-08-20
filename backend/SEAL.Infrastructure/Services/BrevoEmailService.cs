using Microsoft.Extensions.Configuration;
using SEAL_Application.Interfaces;
using System;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;

namespace SEAL_Infrastructure.Services
{
    // Gui email qua Brevo Transactional Email API (HTTPS, khong dung cong SMTP thu).
    // Ly do doi tu SmtpClient (MailKit) sang day: Render chan/nghen ket noi ra cong SMTP
    // 587 (moi lan goi ConnectAsync deu treo dung toi 20s roi timeout, khong phai loi sai
    // tai khoan - sai tai khoan Gmail se tu choi trong 1-2s chu khong treo). Brevo dung HTTPS
    // 443 nen khong bi chan, va chi can xac minh 1 dia chi gui (khong can domain rieng nhu
    // Resend) - phu hop du an khong co domain.
    public class BrevoEmailService : IEmailService
    {
        private readonly HttpClient _httpClient;
        private readonly IConfiguration _config;

        public BrevoEmailService(HttpClient httpClient, IConfiguration config)
        {
            _httpClient = httpClient;
            _config = config;
        }

        public async Task SendEmailAsync(string toEmail, string subject, string body)
        {
            var apiKey = _config["BrevoSettings:ApiKey"] ?? _config["BREVO_API_KEY"];
            var outboxPath = _config["EmailSettings:OutboxPath"] ?? _config["EMAIL_OUTBOX_PATH"];

            bool isPlaceholder = string.IsNullOrWhiteSpace(apiKey) || apiKey.StartsWith("YOUR_", StringComparison.OrdinalIgnoreCase);

            // DEV FALLBACK: chua cau hinh BrevoSettings:ApiKey hoac con la placeholder "YOUR_..."
            // nhung co OutboxPath (thuong chi o Development) -> ghi email ra file HTML de xem truoc.
            if (isPlaceholder && !string.IsNullOrWhiteSpace(outboxPath))
            {
                Directory.CreateDirectory(outboxPath);
                var html = $"<!-- To: {toEmail} | Subject: {subject} | {DateTime.Now:O} -->\n{body}";
                var stamp = DateTime.Now.ToString("yyyyMMdd_HHmmss_fff");
                await File.WriteAllTextAsync(Path.Combine(outboxPath, $"{stamp}.html"), html);
                await File.WriteAllTextAsync(Path.Combine(outboxPath, "latest.html"), html);
                return;
            }

            if (isPlaceholder)
            {
                throw new InvalidOperationException("Chưa cấu hình BrevoSettings:ApiKey hợp lệ.");
            }

            var senderEmail = _config["BrevoSettings:SenderEmail"]
                ?? _config["BREVO_SENDER_EMAIL"]
                ?? "truonghoangphuc27012005@gmail.com";
            var senderName = _config["BrevoSettings:SenderName"] ?? "SEAL Hackathon";

            var payload = new BrevoSendRequest
            {
                Sender = new BrevoContact { Name = senderName, Email = senderEmail },
                To = new[] { new BrevoContact { Email = toEmail } },
                Subject = subject,
                HtmlContent = body,
            };

            using var request = new HttpRequestMessage(HttpMethod.Post, "v3/smtp/email")
            {
                Content = JsonContent.Create(payload),
            };
            request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            request.Headers.TryAddWithoutValidation("api-key", apiKey);

            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(15));
            using var response = await _httpClient.SendAsync(request, cts.Token);

            if (!response.IsSuccessStatusCode)
            {
                var errorBody = await response.Content.ReadAsStringAsync(cts.Token);
                throw new InvalidOperationException(
                    $"Brevo trả lỗi {(int)response.StatusCode}: {errorBody}");
            }
        }

        private class BrevoSendRequest
        {
            [JsonPropertyName("sender")]
            public BrevoContact Sender { get; set; } = new();

            [JsonPropertyName("to")]
            public BrevoContact[] To { get; set; } = Array.Empty<BrevoContact>();

            [JsonPropertyName("subject")]
            public string Subject { get; set; } = string.Empty;

            [JsonPropertyName("htmlContent")]
            public string HtmlContent { get; set; } = string.Empty;
        }

        private class BrevoContact
        {
            [JsonPropertyName("name")]
            public string? Name { get; set; }

            [JsonPropertyName("email")]
            public string Email { get; set; } = string.Empty;
        }
    }
}
