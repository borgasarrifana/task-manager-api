using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using System.Security.Claims;
using TaskManager.Api.Common;
using TaskManager.Api.Data;
using TaskManager.Api.DTOs;
using TaskManager.Api.Models;

namespace TaskManager.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class AccountController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly ILogger<AccountController> _logger;

        public AccountController(AppDbContext context, ILogger<AccountController> logger)
        {
            _context = context;
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

            var email = EmailAddressHelper.Normalize(dto.Email);

            if (email != user.Email)
            {
                if (email != null && await _context.Users.AnyAsync(u => u.Email == email && u.Id != user.Id))
                {
                    return BadRequest("Email already registered.");
                }

                user.Email = email;
                user.EmailConfirmed = false; // a new address must be verified again (Phase 2)
                _logger.LogInformation("User {UserId} changed their email address", user.Id);
            }

            // Reminders need an address to go to
            user.EmailRemindersEnabled = email != null && dto.EmailRemindersEnabled;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateException ex) when (ex.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation })
            {
                // Another account claimed this address between the check above and the save
                return BadRequest("Email already registered.");
            }

            return Ok(ToDto(user));
        }
    }
}