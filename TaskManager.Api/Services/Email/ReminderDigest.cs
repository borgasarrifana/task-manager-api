using TaskManager.Api.Models;

namespace TaskManager.Api.Services.Email
{
    public record DigestItem(string Title, string ProjectName, Priority Priority, DateTime DueDate);

    public record ReminderDigest(
        IReadOnlyList<DigestItem> Overdue,
        IReadOnlyList<DigestItem> DueToday,
        IReadOnlyList<DigestItem> DueTomorrow,
        IReadOnlyList<DigestItem> Upcoming)
    {
        public int Total => Overdue.Count + DueToday.Count + DueTomorrow.Count + Upcoming.Count;
    }
}