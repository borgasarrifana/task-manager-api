namespace TaskManager.Api.Models
{
    public enum ReminderFrequency
    {
        Daily = 0,
        Weekly = 1,      // on selected days of the week
        Fortnightly = 2, // every 15 days
        Monthly = 3
    }
}