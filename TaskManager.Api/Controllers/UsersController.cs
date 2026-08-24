using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using TaskManager.Api.Data;
using TaskManager.Api.DTOs;

namespace TaskManager.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Roles = "Admin")]
    public class UsersController : ControllerBase
    {
        private readonly AppDbContext _context;

        public UsersController(AppDbContext context)
        {
            _context = context;
        }

        private int GetCurrentUserId()
        {
            var idClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            return int.Parse(idClaim!);
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<UserResponseDto>>> GetUsers()
        {
            var users = await _context.Users
                .Select(u => new UserResponseDto
                {
                    Id = u.Id,
                    Username = u.Username,
                    Role = u.Role
                })
                .ToListAsync();

            return Ok(users);
        }

        [HttpGet("{id}/projects")]
        public async Task<ActionResult<IEnumerable<ProjectResponseDto>>> GetUserProjects(int id)
        {
            var userExists = await _context.Users.AnyAsync(u => u.Id == id);
            if (!userExists) return NotFound($"User {id} not found.");

            var projects = await _context.Projects
                .Where(p => p.UserId == id)
                .Select(p => new ProjectResponseDto { Id = p.Id, Name = p.Name })
                .ToListAsync();

            return Ok(projects);
        }

        [HttpPut("{id}/role")]
        public async Task<IActionResult> UpdateUserRole(int id, UpdateUserRoleDto dto)
        {
            var user = await _context.Users.FindAsync(id);
            if (user == null) return NotFound($"User {id} not found.");

            user.Role = dto.Role;
            await _context.SaveChangesAsync();

            return NoContent();
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateUser(int id, UpdateUserDto dto)
        {
            var user = await _context.Users.FindAsync(id);
            if (user == null) return NotFound($"User {id} not found.");

            if (await _context.Users.AnyAsync(u => u.Username == dto.Username && u.Id != id))
            {
                return BadRequest("Username already taken.");
            }

            user.Username = dto.Username;
            await _context.SaveChangesAsync();

            return NoContent();
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteUser(int id)
        {
            if (id == GetCurrentUserId())
            {
                return BadRequest("You cannot delete your own account.");
            }

            var user = await _context.Users.FindAsync(id);
            if (user == null) return NotFound($"User {id} not found.");

            var projects = await _context.Projects.Where(p => p.UserId == id).ToListAsync();
            var projectIds = projects.Select(p => p.Id).ToList();
            var tasks = await _context.Tasks.Where(t => projectIds.Contains(t.ProjectId)).ToListAsync();

            _context.Tasks.RemoveRange(tasks);
            _context.Projects.RemoveRange(projects);
            _context.Users.Remove(user);
            await _context.SaveChangesAsync();

            return NoContent();
        }
    }
}