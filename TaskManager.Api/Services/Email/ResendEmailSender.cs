using Microsoft.Extensions.Options;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using TaskManager.Api.Common;

namespace TaskManager.Api.Services.Email
{
    // Uses Resend's HTTPS API (port 443) — SMTP ports are blocked on Render's free tier
    public class ResendEmailSender : IEmailSender
    {
        private readonly HttpClient _http;
        private readonly EmailOptions _options;

        public ResendEmailSender(HttpClient http, IOptions<EmailOptions> options)
        {
            _http = http;
            _options = options.Value;
            _http.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", _options.ResendApiKey);
        }

        public async Task SendAsync(EmailMessage message, CancellationToken cancellationToken = default)
        {
            var payload = new
            {
                from = _options.From,
                to = new[] { message.To },
                subject = message.Subject,
                html = message.HtmlBody,
                text = message.TextBody
            };

            using var response = await _http.PostAsJsonAsync("emails", payload, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                var body = await response.Content.ReadAsStringAsync(cancellationToken);
                throw new HttpRequestException($"Resend returned {(int)response.StatusCode}: {body}");
            }
        }
    }
}