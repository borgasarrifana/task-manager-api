namespace TaskManager.Api.Models
{
    public class User
    {
        public int Id { get; set; }
        public string Username { get; set; } = string.Empty;
        public string PasswordHash { get; set; } = string.Empty;
        public UserRole Role { get; set; } = UserRole.Member;

        // Stored normalized (trimmed, lowercase). Null for accounts created before emails existed.
        public string? Email { get; set; }
        public bool EmailConfirmed { get; set; }
        public bool EmailRemindersEnabled { get; set; }

        // Pending email verification (only the SHA-256 hash of the token is stored)
        public string? EmailVerificationTokenHash { get; set; }
        public DateTime? EmailVerificationExpiresAt { get; set; }
        public DateTime? EmailVerificationSentAt { get; set; }

        // UTC date of the last reminder digest sent (makes the daily job idempotent)
        public DateOnly? LastReminderSentOn { get; set; }
        public ReminderFrequency ReminderFrequency { get; set; } = ReminderFrequency.Daily;

        // Bitmask of days for Weekly: bit n = (DayOfWeek)n, so Sunday = 1, Monday = 2 ... Saturday = 64
        public int ReminderDays { get; set; }
    }
}