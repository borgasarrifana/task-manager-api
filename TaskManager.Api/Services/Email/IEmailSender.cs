namespace TaskManager.Api.Services.Email
{
    public record EmailMessage(string To, string Subject, string HtmlBody, string TextBody);

    public interface IEmailSender
    {
        Task SendAsync(EmailMessage message, CancellationToken cancellationToken = default);
    }
}