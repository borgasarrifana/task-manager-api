namespace TaskManager.Api.Common
{
    public static class EmailAddressHelper
    {
        // Single place that defines how emails are stored and compared
        public static string? Normalize(string? email) =>
            string.IsNullOrWhiteSpace(email) ? null : email.Trim().ToLowerInvariant();
    }
}