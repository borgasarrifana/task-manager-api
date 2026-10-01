using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using TaskManager.Api.Common;
using TaskManager.Api.Data;
using TaskManager.Api.Models;
using TaskManager.Api.Services;
using TaskManager.Api.Services.Email;
using Xunit;

namespace TaskManager.Api.Tests.Services
{
    public class EmailVerificationServiceTests
    {
        private static readonly DateTimeOffset Now = new(2026, 9, 27, 10, 0, 0, TimeSpan.Zero);

        private sealed class FixedTimeProvider(DateTimeOffset now) : TimeProvider
        {
            public DateTimeOffset Current { get; set; } = now;
            public override DateTimeOffset GetUtcNow() => Current;
        }

        private sealed class FakeEmailSender : IEmailSender
        {
            public List<EmailMessage> Sent { get; } = new();
            public Task SendAsync(EmailMessage message, CancellationToken cancellationToken = default)
            {
                Sent.Add(message);
                return Task.CompletedTask;
            }
        }

        private static (EmailVerificationService Service, FakeEmailSender Sender, FixedTimeProvider Time) Create(AppDbContext context)
        {
            var sender = new FakeEmailSender();
            var time = new FixedTimeProvider(Now);
            var service = new EmailVerificationService(
                context,
                sender,
                Options.Create(new AppOptions { FrontendBaseUrl = "https://app.test" }),
                time,
                NullLogger<EmailVerificationService>.Instance);
            return (service, sender, time);
        }

        private static string ExtractToken(EmailMessage message)
        {
            const string marker = "verify-email=";
            var start = message.TextBody.IndexOf(marker) + marker.Length;
            return message.TextBody.Substring(start, 64); // 32 bytes as hex
        }

        [Fact]
        public async Task SendVerificationEmailAsync_StoresHashNotRawToken_AndEmailsLink()
        {
            using var context = TestDbContextFactory.Create();
            var user = new User { Id = 1, Username = "alice", Email = "alice@example.com" };
            context.Users.Add(user);
            await context.SaveChangesAsync();
            var (service, sender, _) = Create(context);

            var sent = await service.SendVerificationEmailAsync(user);

            Assert.True(sent);
            var message = Assert.Single(sender.Sent);
            Assert.Equal("alice@example.com", message.To);
            Assert.Contains("https://app.test/?verify-email=", message.TextBody);

            var rawToken = ExtractToken(message);
            Assert.NotEqual(rawToken, user.EmailVerificationTokenHash);
            Assert.Equal(SecureTokens.Hash(rawToken), user.EmailVerificationTokenHash);
        }

        [Fact]
        public async Task VerifyAsync_ValidToken_ConfirmsEmailAndIsSingleUse()
        {
            using var context = TestDbContextFactory.Create();
            var user = new User { Id = 1, Username = "alice", Email = "alice@example.com" };
            context.Users.Add(user);
            await context.SaveChangesAsync();
            var (service, sender, _) = Create(context);
            await service.SendVerificationEmailAsync(user);
            var token = ExtractToken(sender.Sent[0]);

            Assert.True(await service.VerifyAsync(token));
            Assert.True(user.EmailConfirmed);
            Assert.False(await service.VerifyAsync(token)); // second use fails
        }

        [Fact]
        public async Task VerifyAsync_ExpiredToken_ReturnsFalse()
        {
            using var context = TestDbContextFactory.Create();
            var user = new User { Id = 1, Username = "alice", Email = "alice@example.com" };
            context.Users.Add(user);
            await context.SaveChangesAsync();
            var (service, sender, time) = Create(context);
            await service.SendVerificationEmailAsync(user);
            var token = ExtractToken(sender.Sent[0]);

            time.Current = Now.AddHours(25);

            Assert.False(await service.VerifyAsync(token));
            Assert.False(user.EmailConfirmed);
        }

        [Fact]
        public async Task VerifyAsync_UnknownToken_ReturnsFalse()
        {
            using var context = TestDbContextFactory.Create();
            var (service, _, _) = Create(context);

            Assert.False(await service.VerifyAsync(SecureTokens.Generate()));
        }
    }
}