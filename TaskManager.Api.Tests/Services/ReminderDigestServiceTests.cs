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
    public class ReminderDigestServiceTests
    {
        private static readonly DateTimeOffset Now = new(2026, 10, 1, 6, 30, 0, TimeSpan.Zero);

        private sealed class FixedTimeProvider(DateTimeOffset now) : TimeProvider
        {
            public override DateTimeOffset GetUtcNow() => now;
        }

        private sealed class FakeEmailSender : IEmailSender
        {
            public List<EmailMessage> Sent { get; } = new();
            public bool Fail { get; set; }

            public Task SendAsync(EmailMessage message, CancellationToken cancellationToken = default)
            {
                if (Fail) throw new HttpRequestException("provider down");
                Sent.Add(message);
                return Task.CompletedTask;
            }
        }

        private static DateTime Utc(int month, int day) => new(2026, month, day, 0, 0, 0, DateTimeKind.Utc);

        private static ReminderDigestService CreateService(AppDbContext context, FakeEmailSender sender) =>
            new(context,
                sender,
                Options.Create(new AppOptions { FrontendBaseUrl = "https://app.test" }),
                new FixedTimeProvider(Now),
                NullLogger<ReminderDigestService>.Instance,
                TimeSpan.Zero);

        private static void SeedStandardScenario(AppDbContext context)
        {
            context.Users.AddRange(
                new User { Id = 1, Username = "alice", Email = "alice@example.com", EmailConfirmed = true, EmailRemindersEnabled = true },
                new User { Id = 2, Username = "bob", Email = "bob@example.com", EmailConfirmed = false, EmailRemindersEnabled = true },  // unverified
                new User { Id = 3, Username = "carol", Email = "carol@example.com", EmailConfirmed = true, EmailRemindersEnabled = false } // opted out
            );
            context.Projects.AddRange(
                new Project
                {
                    Id = 1, Name = "Launch", UserId = 1,
                    Tasks = new List<TaskItem>
                    {
                        new() { Title = "Overdue task", DueDate = Utc(9, 28) },
                        new() { Title = "Today task", DueDate = Utc(10, 1) },
                        new() { Title = "Tomorrow task", DueDate = Utc(10, 2) },
                        new() { Title = "Next week task", DueDate = Utc(10, 8) },
                        new() { Title = "Done overdue task", DueDate = Utc(9, 28), IsDone = true }
                    }
                },
                new Project
                {
                    Id = 2, Name = "Archived", UserId = 1, IsCompleted = true,
                    Tasks = new List<TaskItem> { new() { Title = "Completed project task", DueDate = Utc(9, 28), IsDone = true } }
                },
                new Project
                {
                    Id = 3, Name = "Bob's", UserId = 2,
                    Tasks = new List<TaskItem> { new() { Title = "Bob task", DueDate = Utc(10, 1) } }
                },
                new Project
                {
                    Id = 4, Name = "Carol's", UserId = 3,
                    Tasks = new List<TaskItem> { new() { Title = "Carol task", DueDate = Utc(10, 1) } }
                }
            );
        }

        [Fact]
        public async Task SendDueRemindersAsync_SendsOnlyToVerifiedOptedInUsers_WithDueTasks()
        {
            using var context = TestDbContextFactory.Create();
            SeedStandardScenario(context);
            await context.SaveChangesAsync();
            var sender = new FakeEmailSender();

            var result = await CreateService(context, sender).SendDueRemindersAsync();

            Assert.Equal(1, result.EligibleUsers);
            Assert.Equal(1, result.Sent);
            var email = Assert.Single(sender.Sent);
            Assert.Equal("alice@example.com", email.To);
        }

        [Fact]
        public async Task SendDueRemindersAsync_DigestIncludesOverdueTodayTomorrow_ExcludesDoneAndLater()
        {
            using var context = TestDbContextFactory.Create();
            SeedStandardScenario(context);
            await context.SaveChangesAsync();
            var sender = new FakeEmailSender();

            await CreateService(context, sender).SendDueRemindersAsync();

            var body = Assert.Single(sender.Sent).TextBody;
            Assert.Contains("OVERDUE (1)", body);
            Assert.Contains("DUE TODAY (1)", body);
            Assert.Contains("DUE TOMORROW (1)", body);
            Assert.DoesNotContain("Next week task", body);
            Assert.DoesNotContain("Done overdue task", body);
            Assert.DoesNotContain("Completed project task", body);
        }

        [Fact]
        public async Task SendDueRemindersAsync_SecondRunSameDay_SendsNothing()
        {
            using var context = TestDbContextFactory.Create();
            SeedStandardScenario(context);
            await context.SaveChangesAsync();
            var sender = new FakeEmailSender();
            var service = CreateService(context, sender);

            await service.SendDueRemindersAsync();
            var second = await service.SendDueRemindersAsync();

            Assert.Equal(0, second.EligibleUsers);
            Assert.Single(sender.Sent);
        }

        [Fact]
        public async Task SendDueRemindersAsync_SendFailure_IsNotMarkedAsSent()
        {
            using var context = TestDbContextFactory.Create();
            SeedStandardScenario(context);
            await context.SaveChangesAsync();
            var sender = new FakeEmailSender { Fail = true };

            var result = await CreateService(context, sender).SendDueRemindersAsync();

            Assert.Equal(1, result.Failed);
            var alice = await context.Users.FindAsync(1);
            Assert.Null(alice!.LastReminderSentOn); // will be retried on the next run
        }

        [Fact]
        public async Task SendDueRemindersAsync_NothingDue_SkipsWithoutMarking()
        {
            using var context = TestDbContextFactory.Create();
            context.Users.Add(new User { Id = 1, Username = "alice", Email = "alice@example.com", EmailConfirmed = true, EmailRemindersEnabled = true });
            context.Projects.Add(new Project
            {
                Id = 1, Name = "Calm", UserId = 1,
                Tasks = new List<TaskItem> { new() { Title = "Far away", DueDate = Utc(12, 1) } }
            });
            await context.SaveChangesAsync();
            var sender = new FakeEmailSender();

            var result = await CreateService(context, sender).SendDueRemindersAsync();

            Assert.Equal(1, result.NothingDue);
            Assert.Empty(sender.Sent);
            Assert.Null((await context.Users.FindAsync(1))!.LastReminderSentOn);
        }

                [Fact]
        public async Task SendDueRemindersAsync_Fortnightly_IncludesTasksUntilNextDigest()
        {
            using var context = TestDbContextFactory.Create();
            context.Users.Add(new User
            {
                Id = 1, Username = "alice", Email = "alice@example.com",
                EmailConfirmed = true, EmailRemindersEnabled = true,
                ReminderFrequency = ReminderFrequency.Fortnightly
            });
            context.Projects.Add(new Project
            {
                Id = 1, Name = "Launch", UserId = 1,
                Tasks = new List<TaskItem>
                {
                    new() { Title = "In ten days", DueDate = Utc(10, 11) },
                    new() { Title = "In twenty days", DueDate = Utc(10, 21) }
                }
            });
            await context.SaveChangesAsync();
            var sender = new FakeEmailSender();

            await CreateService(context, sender).SendDueRemindersAsync();

            var email = Assert.Single(sender.Sent);
            Assert.Contains("COMING UP (1)", email.TextBody);
            Assert.Contains("In ten days", email.TextBody);
            Assert.DoesNotContain("In twenty days", email.TextBody);
            Assert.Contains("Fortnightly Briefing", email.HtmlBody);
        }
    }
}