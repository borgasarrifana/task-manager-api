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

        private static DateTime StartOfDayUtc(DateOnly day) => day.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);

        public async Task<ReminderRunResult> SendDueRemindersAsync(CancellationToken cancellationToken = default)
        {
            var todayDate = DateOnly.FromDateTime(_time.GetUtcNow().UtcDateTime);
            var today = StartOfDayUtc(todayDate);
            var tomorrow = today.AddDays(1);
            var dayAfterTomorrow = today.AddDays(2);

            // Query 1: opted-in, verified users; schedules are then checked in memory
            var candidates = await _context.Users
                .Where(u => u.Email != null && u.EmailConfirmed && u.EmailRemindersEnabled)
                .ToListAsync(cancellationToken);

            var users = candidates
                .Where(u => ReminderSchedule.IsDue(u.ReminderFrequency, u.ReminderDays, u.LastReminderSentOn, todayDate))
                .ToList();

            if (users.Count == 0)
            {
                _logger.LogInformation("Reminder run: no users due today");
                return new ReminderRunResult(0, 0, 0, 0);
            }

            // Each digest covers everything due before the user's next digest, including that day
            var horizons = users.ToDictionary(
                u => u.Id,
                u => StartOfDayUtc(ReminderSchedule.NextDue(u.ReminderFrequency, u.ReminderDays, todayDate)).AddDays(1));
            var maxHorizon = horizons.Values.Max();
            var userIds = users.Select(u => u.Id).ToList();

            // Query 2: all their open tasks due within the furthest horizon, in one round trip
            var dueTasks = await (
                from t in _context.Tasks.AsNoTracking()
                join p in _context.Projects.AsNoTracking() on t.ProjectId equals p.Id
                where userIds.Contains(p.UserId)
                      && !p.IsCompleted
                      && !t.IsDone
                      && t.DueDate != null
                      && t.DueDate < maxHorizon
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
                var horizon = horizons[user.Id];
                var items = tasksByUser[user.Id].Where(i => i.DueDate < horizon).ToList();
                if (items.Count == 0)
                {
                    nothingDue++;
                    continue;
                }

                var digest = new ReminderDigest(
                    Overdue: items.Where(i => i.DueDate < today).ToList(),
                    DueToday: items.Where(i => i.DueDate >= today && i.DueDate < tomorrow).ToList(),
                    DueTomorrow: items.Where(i => i.DueDate >= tomorrow && i.DueDate < dayAfterTomorrow).ToList(),
                    Upcoming: items.Where(i => i.DueDate >= dayAfterTomorrow).ToList());

                if (!isFirstSend && _delayBetweenSends > TimeSpan.Zero)
                {
                    await Task.Delay(_delayBetweenSends, cancellationToken);
                }
                isFirstSend = false;

                try
                {
                    await _emailSender.SendAsync(
                        EmailTemplates.DueReminder(user.Email!, user.Username, digest, _appOptions.FrontendBaseUrl, user.ReminderFrequency),
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
                "Reminder run: {Due} due, {Sent} sent, {NothingDue} with nothing due, {Failed} failed",
                users.Count, sent, nothingDue, failed);

            return new ReminderRunResult(users.Count, sent, nothingDue, failed);
        }
    }
}