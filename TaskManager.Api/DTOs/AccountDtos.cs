using System.ComponentModel.DataAnnotations;
using TaskManager.Api.Models;

namespace TaskManager.Api.DTOs
{
    public class AccountResponseDto
    {
        public string Username { get; set; } = string.Empty;
        public string? Email { get; set; }
        public bool EmailConfirmed { get; set; }
        public bool EmailRemindersEnabled { get; set; }
        public ReminderFrequency ReminderFrequency { get; set; }
        public List<DayOfWeek> ReminderDays { get; set; } = new();
        public UserRole Role { get; set; }
    }

    public class UpdateAccountDto
    {
        // Null or empty removes the email
        [EmailAddress]
        [StringLength(254)]
        public string? Email { get; set; }

        public bool EmailRemindersEnabled { get; set; }

        public ReminderFrequency ReminderFrequency { get; set; } = ReminderFrequency.Daily;

        // Used when ReminderFrequency is Weekly
        public List<DayOfWeek>? ReminderDays { get; set; }
    }

    public class VerifyEmailDto
    {
        [Required]
        public string Token { get; set; } = string.Empty;
    }
}