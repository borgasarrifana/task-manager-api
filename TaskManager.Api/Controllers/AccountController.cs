using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using System.Security.Claims;
using TaskManager.Api.Common;
using TaskManager.Api.Data;
using TaskManager.Api.DTOs;
using TaskManager.Api.Models;
using TaskManager.Api.Services;

namespace TaskManager.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class AccountController : ControllerBase
    {
        private static readonly TimeSpan ResendCooldown = TimeSpan.FromMinutes(1);

        private readonly AppDbContext _context;
        private readonly IEmailVerificationService _verification;
        private readonly TimeProvider _time;
        private readonly ILogger<AccountController> _logger;

        public AccountController(
            AppDbContext context,
            IEmailVerificationService verification,
            TimeProvider time,
            ILogger<AccountController> logger)
        {
            _context = context;
            _verification = verification;
            _time = time;
            _logger = logger;
        }

        private int GetUserId()
        {
            var idClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            return int.Parse(idClaim!);
        }

        private static AccountResponseDto ToDto(User user) => new()
        {
            Username = user.Username,
            Email = user.Email,
            EmailConfirmed = user.EmailConfirmed,
            EmailRemindersEnabled = user.EmailRemindersEnabled,
            ReminderFrequency = user.ReminderFrequency,
            ReminderDays = ReminderSchedule.FromMask(user.ReminderDays),
            Role = user.Role
        };

        [HttpGet]
        public async Task<ActionResult<AccountResponseDto>> GetAccount()
        {
            var user = await _context.Users.FindAsync(GetUserId());
            if (user == null) return NotFound();
            return Ok(ToDto(user));
        }

        [HttpPut]
        public async Task<ActionResult<AccountResponseDto>> UpdateAccount(UpdateAccountDto dto)
        {
            var user = await _context.Users.FindAsync(GetUserId());
            if (user == null) return NotFound();

            if (!Enum.IsDefined(dto.ReminderFrequency))
            {
                return BadRequest("Unknown reminder frequency.");
            }

            var dayMask = ReminderSchedule.ToMask(dto.ReminderDays ?? new List<DayOfWeek>());
            if (dto.ReminderFrequency == ReminderFrequency.Weekly && dayMask == 0)
            {
                return BadRequest("Choose at least one day for reminders.");
            }

            var email = EmailAddressHelper.Normalize(dto.Email);
            var emailChanged = email != user.Email;

            if (emailChanged)
            {
                if (email != null && await _context.Users.AnyAsync(u => u.Email == email && u.Id != user.Id))
                {
                    return BadRequest("Email already registered.");
                }

                user.Email = email;
                user.EmailConfirmed = false;
                user.EmailVerificationTokenHash = null; // a link for the old address must stop working
                user.EmailVerificationExpiresAt = null;
                _logger.LogInformation("User {UserId} changed their email address", user.Id);
            }

            user.EmailRemindersEnabled = email != null && dto.EmailRemindersEnabled;
            user.ReminderFrequency = dto.ReminderFrequency;
            user.ReminderDays = dayMask; // kept even when not Weekly, so switching back restores the choice

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateException ex) when (ex.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation })
            {
                return BadRequest("Email already registered.");
            }

            if (emailChanged && email != null)
            {
                await _verification.SendVerificationEmailAsync(user);
            }

            return Ok(ToDto(user));
        }

        [HttpPost("resend-verification")]
        public async Task<IActionResult> ResendVerification()
        {
            var user = await _context.Users.FindAsync(GetUserId());
            if (user == null) return NotFound();
            if (user.Email == null) return BadRequest("Add an email address first.");
            if (user.EmailConfirmed) return BadRequest("Your email is already verified.");

            var now = _time.GetUtcNow().UtcDateTime;
            if (user.EmailVerificationSentAt != null && now - user.EmailVerificationSentAt < ResendCooldown)
            {
                return StatusCode(StatusCodes.Status429TooManyRequests,
                    "Please wait a minute before requesting another email.");
            }

            var sent = await _verification.SendVerificationEmailAsync(user);
            return sent
                ? NoContent()
                : StatusCode(StatusCodes.Status503ServiceUnavailable, "Could not send the email right now. Try again shortly.");
        }

        // Anonymous: the link is often opened on a device where the user isn't logged in
        [AllowAnonymous]
        [HttpPost("verify-email")]
        public async Task<IActionResult> VerifyEmail(VerifyEmailDto dto)
        {
            var verified = await _verification.VerifyAsync(dto.Token);
            return verified
                ? Ok("Email verified.")
                : BadRequest("This verification link is invalid or has expired.");
        }
    }
}