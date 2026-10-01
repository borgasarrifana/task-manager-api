using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using System.Security.Cryptography;
using System.Text;
using TaskManager.Api.Common;
using TaskManager.Api.Services;

namespace TaskManager.Api.Controllers
{
    // Machine-to-machine endpoints triggered by a scheduler (GitHub Actions), not by users
    [ApiController]
    [Route("api/jobs")]
    [AllowAnonymous]
    public class JobsController : ControllerBase
    {
        private readonly IReminderDigestService _reminders;
        private readonly JobOptions _options;
        private readonly ILogger<JobsController> _logger;

        public JobsController(IReminderDigestService reminders, IOptions<JobOptions> options, ILogger<JobsController> logger)
        {
            _reminders = reminders;
            _options = options.Value;
            _logger = logger;
        }

        [HttpPost("due-reminders")]
        public async Task<ActionResult<ReminderRunResult>> RunDueReminders(
            [FromHeader(Name = "X-Job-Key")] string? jobKey,
            CancellationToken cancellationToken)
        {
            if (!IsAuthorized(jobKey))
            {
                _logger.LogWarning("Rejected job call to due-reminders: missing or invalid key");
                return Unauthorized();
            }

            var result = await _reminders.SendDueRemindersAsync(cancellationToken);
            return Ok(result);
        }

        private bool IsAuthorized(string? provided)
        {
            // No key configured = jobs disabled
            if (string.IsNullOrEmpty(_options.Key) || string.IsNullOrEmpty(provided)) return false;

            // Constant-time comparison: response timing can't reveal how much of the key matched
            return CryptographicOperations.FixedTimeEquals(
                Encoding.UTF8.GetBytes(provided),
                Encoding.UTF8.GetBytes(_options.Key));
        }
    }
}