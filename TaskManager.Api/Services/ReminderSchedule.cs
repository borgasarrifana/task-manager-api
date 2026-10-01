using TaskManager.Api.Models;

namespace TaskManager.Api.Services
{
    public static class ReminderSchedule
    {
        public const int FortnightDays = 15;
        private const int MaxLookaheadDays = 62;

        // Is a digest due today, given when the last one was sent?
        public static bool IsDue(ReminderFrequency frequency, int dayMask, DateOnly? lastSentOn, DateOnly today)
        {
            if (lastSentOn >= today) return false; // at most one per day

            return frequency switch
            {
                ReminderFrequency.Daily => true,
                ReminderFrequency.Weekly => IncludesDay(dayMask, today.DayOfWeek),
                ReminderFrequency.Fortnightly => lastSentOn == null || today >= lastSentOn.Value.AddDays(FortnightDays),
                ReminderFrequency.Monthly => lastSentOn == null || today >= lastSentOn.Value.AddMonths(1),
                _ => false
            };
        }

        // The date of the next digest if one is sent on `sentOn` (reuses IsDue, so the rules live in one place)
        public static DateOnly NextDue(ReminderFrequency frequency, int dayMask, DateOnly sentOn)
        {
            for (var day = sentOn.AddDays(1); day <= sentOn.AddDays(MaxLookaheadDays); day = day.AddDays(1))
            {
                if (IsDue(frequency, dayMask, sentOn, day)) return day;
            }
            return sentOn.AddMonths(1); // e.g. Weekly with no days selected
        }

        public static bool IncludesDay(int dayMask, DayOfWeek day) => (dayMask & (1 << (int)day)) != 0;

        public static int ToMask(IEnumerable<DayOfWeek> days) =>
            days.Aggregate(0, (mask, day) => mask | (1 << (int)day));

        public static List<DayOfWeek> FromMask(int dayMask) =>
            Enum.GetValues<DayOfWeek>().Where(d => IncludesDay(dayMask, d)).ToList();
    }
}