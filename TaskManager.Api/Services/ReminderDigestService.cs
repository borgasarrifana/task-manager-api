using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using TaskManager.Api.Common;
using TaskManager.Api.Data;
using TaskManager.Api.Services.Email;

namespace TaskManager.Api.Services
{
    public class ReminderDigestService : IReminderDigestService
    {
        private readonly AppDbContext _context;
        private readonly IEmailSender _emailSender;
        private readonly AppOptions _appOptions;
        private readonly TimeProvider _time;
        private readonly ILogger<ReminderDigestService> _logger;
        private readonly TimeSpan _delayBetweenSends;

        public ReminderDigestService(
            AppDbContext context,
            IEmailSender emailSender,
            IOptions<AppOptions> appOptions,
            TimeProvider time,
            ILogger<ReminderDigestService> logger,
            TimeSpan? delayBetweenSends = null) // tests pass TimeSpan.Zero
        {
            _context = context;
            _emailSender = emailSender;
            _appOptions = appOptions.Value;
            _time = time;
            _logger = logger;
            // Stays under the email provider's per-second rate limit
            _delayBetweenSends = delayBetweenSends ?? TimeSpan.FromMilliseconds(600);
        }

        public async Task<ReminderRunResult> SendDueRemindersAsync(CancellationToken cancellationToken = default)
        {
            var today = _time.GetUtcNow().UtcDateTime.Date;
            var tomorrow = today.AddDays(1);
            var dayAfterTomorrow = today.AddDays(2);
            var todayDate = DateOnly.FromDateTime(today);

            // Query 1: who is eligible and hasn't had today's digest yet
            var users = await _context.Users
                .Where(u => u.Email != null
                    && u.EmailConfirmed
                    && u.EmailRemindersEnabled
                    && (u.LastReminderSentOn == null || u.LastReminderSentOn < todayDate))
                .ToListAsync(cancellationToken);

            if (users.Count == 0)
            {
                _logger.LogInformation("Reminder run: no eligible users");
                return new ReminderRunResult(0, 0, 0, 0);
            }

            var userIds = users.Select(u => u.Id).ToList();

            // Query 2: all their open, due-soon tasks in active projects, in one round trip
            var dueTasks = await (
                from t in _context.Tasks.AsNoTracking()
                join p in _context.Projects.AsNoTracking() on t.ProjectId equals p.Id
                where userIds.Contains(p.UserId)
                      && !p.IsCompleted
                      && !t.IsDone
                      && t.DueDate != null
                      && t.DueDate < dayAfterTomorrow
                orderby t.DueDate, t.Title
                select new
                {
                    p.UserId,
                    t.Title,
                    ProjectName = p.Name,
                    t.Priority,
                    DueDate = t.DueDate!.Value
                })
                .ToListAsync(cancellationToken);

            var tasksByUser = dueTasks.ToLookup(
                x => x.UserId,
                x => new DigestItem(x.Title, x.ProjectName, x.Priority, x.DueDate));

            int sent = 0, nothingDue = 0, failed = 0;
            var isFirstSend = true;

            foreach (var user in users)
            {
                var items = tasksByUser[user.Id].ToList();
                if (items.Count == 0)
                {
                    nothingDue++;
                    continue;
                }

                var digest = new ReminderDigest(
                    Overdue: items.Where(i => i.DueDate < today).ToList(),
                    DueToday: items.Where(i => i.DueDate >= today && i.DueDate < tomorrow).ToList(),
                    DueTomorrow: items.Where(i => i.DueDate >= tomorrow).ToList());

                if (!isFirstSend && _delayBetweenSends > TimeSpan.Zero)
                {
                    await Task.Delay(_delayBetweenSends, cancellationToken);
                }
                isFirstSend = false;

                try
                {
                    await _emailSender.SendAsync(
                        EmailTemplates.DueReminder(user.Email!, user.Username, digest, _appOptions.FrontendBaseUrl),
                        cancellationToken);

                    // Mark only after a successful send, so failures are retried next run
                    user.LastReminderSentOn = todayDate;
                    await _context.SaveChangesAsync(cancellationToken);
                    sent++;
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    failed++;
                    _logger.LogError(ex, "Failed to send reminder digest to user {UserId}", user.Id);
                }
            }

            _logger.LogInformation(
                "Reminder run: {Eligible} eligible, {Sent} sent, {NothingDue} with nothing due, {Failed} failed",
                users.Count, sent, nothingDue, failed);

            return new ReminderRunResult(users.Count, sent, nothingDue, failed);
        }
    }
}