using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using TaskManager.Api.Common;
using TaskManager.Api.Data;
using TaskManager.Api.Models;
using TaskManager.Api.Services.Email;

namespace TaskManager.Api.Services
{
    public class EmailVerificationService : IEmailVerificationService
    {
        private static readonly TimeSpan TokenLifetime = TimeSpan.FromHours(24);

        private readonly AppDbContext _context;
        private readonly IEmailSender _emailSender;
        private readonly AppOptions _appOptions;
        private readonly TimeProvider _time;
        private readonly ILogger<EmailVerificationService> _logger;

        public EmailVerificationService(
            AppDbContext context,
            IEmailSender emailSender,
            IOptions<AppOptions> appOptions,
            TimeProvider time,
            ILogger<EmailVerificationService> logger)
        {
            _context = context;
            _emailSender = emailSender;
            _appOptions = appOptions.Value;
            _time = time;
            _logger = logger;
        }

        public async Task<bool> SendVerificationEmailAsync(User user, CancellationToken cancellationToken = default)
        {
            if (user.Email == null || user.EmailConfirmed) return false;

            var rawToken = SecureTokens.Generate();
            var now = _time.GetUtcNow().UtcDateTime;

            user.EmailVerificationTokenHash = SecureTokens.Hash(rawToken);
            user.EmailVerificationExpiresAt = now.Add(TokenLifetime);
            user.EmailVerificationSentAt = now;
            await _context.SaveChangesAsync(cancellationToken);

            var link = $"{_appOptions.FrontendBaseUrl.TrimEnd('/')}/?verify-email={rawToken}";

            try
            {
                await _emailSender.SendAsync(
                    EmailTemplates.Verification(user.Email, user.Username, link),
                    cancellationToken);
                _logger.LogInformation("Verification email sent to user {UserId}", user.Id);
                return true;
            }
            catch (Exception ex)
            {
                // The account change already succeeded; the user can request another email
                _logger.LogError(ex, "Failed to send verification email to user {UserId}", user.Id);
                return false;
            }
        }

        public async Task<bool> VerifyAsync(string token, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(token)) return false;

            var hash = SecureTokens.Hash(token.Trim().ToLowerInvariant());
            var now = _time.GetUtcNow().UtcDateTime;

            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.EmailVerificationTokenHash == hash, cancellationToken);

            if (user == null || user.EmailVerificationExpiresAt == null || user.EmailVerificationExpiresAt < now)
            {
                return false;
            }

            user.EmailConfirmed = true;
            user.EmailVerificationTokenHash = null; // single use
            user.EmailVerificationExpiresAt = null;
            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("User {UserId} verified their email", user.Id);
            return true;
        }
    }
}