namespace TaskManager.Api.Common
{
    public class AppOptions
    {
        public string FrontendBaseUrl { get; set; } = "http://localhost:5173";
    }

    public class EmailOptions
    {
        public string Provider { get; set; } = "Log"; // "Log" | "Resend"
        public string From { get; set; } = "Task Manager <notifications@example.com>";
        public string? ResendApiKey { get; set; }
    }

    public class JobOptions
    {
        // Shared secret for scheduled job endpoints (X-Job-Key header). Empty = jobs disabled.
        public string? Key { get; set; }
    }
}