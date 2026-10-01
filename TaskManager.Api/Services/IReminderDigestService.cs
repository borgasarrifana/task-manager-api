namespace TaskManager.Api.Services
{
    public record ReminderRunResult(int EligibleUsers, int Sent, int NothingDue, int Failed);

    public interface IReminderDigestService
    {
        Task<ReminderRunResult> SendDueRemindersAsync(CancellationToken cancellationToken = default);
    }
}