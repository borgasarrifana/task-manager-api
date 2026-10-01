using TaskManager.Api.Models;
using TaskManager.Api.Services;
using Xunit;

namespace TaskManager.Api.Tests.Services
{
    public class ReminderScheduleTests
    {
        // 1 Oct 2026 is a Thursday
        private static DateOnly D(int month, int day) => new(2026, month, day);
        private static readonly int MonThu = ReminderSchedule.ToMask(new[] { DayOfWeek.Monday, DayOfWeek.Thursday });

        [Fact]
        public void Daily_DueOncePerDay()
        {
            Assert.True(ReminderSchedule.IsDue(ReminderFrequency.Daily, 0, D(9, 30), D(10, 1)));
            Assert.False(ReminderSchedule.IsDue(ReminderFrequency.Daily, 0, D(10, 1), D(10, 1)));
        }

        [Fact]
        public void Weekly_DueOnlyOnSelectedDays()
        {
            Assert.True(ReminderSchedule.IsDue(ReminderFrequency.Weekly, MonThu, null, D(10, 1)));   // Thursday
            Assert.False(ReminderSchedule.IsDue(ReminderFrequency.Weekly, MonThu, null, D(9, 30)));  // Wednesday
        }

        [Fact]
        public void Fortnightly_DueFifteenDaysAfterLastDigest()
        {
            Assert.False(ReminderSchedule.IsDue(ReminderFrequency.Fortnightly, 0, D(9, 20), D(10, 4)));
            Assert.True(ReminderSchedule.IsDue(ReminderFrequency.Fortnightly, 0, D(9, 20), D(10, 5)));
        }

        [Fact]
        public void Monthly_DueOnSameDateNextMonth()
        {
            Assert.False(ReminderSchedule.IsDue(ReminderFrequency.Monthly, 0, D(9, 1), D(9, 30)));
            Assert.True(ReminderSchedule.IsDue(ReminderFrequency.Monthly, 0, D(9, 1), D(10, 1)));
        }

        [Fact]
        public void FirstDigest_SendsOnFirstRun_ForIntervalSchedules()
        {
            Assert.True(ReminderSchedule.IsDue(ReminderFrequency.Fortnightly, 0, null, D(10, 1)));
            Assert.True(ReminderSchedule.IsDue(ReminderFrequency.Monthly, 0, null, D(10, 1)));
        }

        [Fact]
        public void NextDue_Weekly_FindsNextSelectedDay()
        {
            Assert.Equal(D(10, 5), ReminderSchedule.NextDue(ReminderFrequency.Weekly, MonThu, D(10, 1))); // Thu -> Mon
        }

        [Fact]
        public void Mask_RoundTrips()
        {
            Assert.Equal(new[] { DayOfWeek.Monday, DayOfWeek.Thursday }, ReminderSchedule.FromMask(MonThu));
        }
    }
}