namespace TaskManager.Api.Services.Email
{
    // Development only: writes emails to the log instead of sending them
    public class LoggingEmailSender : IEmailSender
    {
        private readonly ILogger<LoggingEmailSender> _logger;

        public LoggingEmailSender(ILogger<LoggingEmailSender> logger)
        {
            _logger = logger;
        }

        public Task SendAsync(EmailMessage message, CancellationToken cancellationToken = default)
        {
            _logger.LogInformation(
                "[DEV EMAIL] To: {To} | Subject: {Subject}\n{Body}",
                message.To, message.Subject, message.TextBody);
            return Task.CompletedTask;
        }
    }
}