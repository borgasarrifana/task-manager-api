using TaskManager.Api.Models;

namespace TaskManager.Api.Services
{
    public interface IEmailVerificationService
    {
        // Issues a new token (replacing any pending one) and emails the link. Returns false if sending failed.
        Task<bool> SendVerificationEmailAsync(User user, CancellationToken cancellationToken = default);

        // Returns true if the token was valid and the user's email is now confirmed.
        Task<bool> VerifyAsync(string token, CancellationToken cancellationToken = default);
    }
}