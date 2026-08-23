using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using TaskManager.Api.DTOs;
using TaskManager.Api.Services;
using Microsoft.AspNetCore.Authorization;

namespace TaskManager.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class TasksController : ControllerBase
    {
        private readonly ITaskService _taskService;

        public TasksController(ITaskService taskService)
        {
            _taskService = taskService;
        }

        private int GetUserId()
        {
            var idClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            return int.Parse(idClaim!);
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<TaskResponseDto>> GetTask(int id)
        {
            var userId = GetUserId();
            var task = await _taskService.GetTaskByIdAsync(id, userId);
            if (task == null) return NotFound();

            return Ok(new TaskResponseDto
            {
                Id = task.Id,
                Title = task.Title,
                IsDone = task.IsDone,
                CreatedAt = task.CreatedAt,
                Priority = task.Priority,
                DueDate = task.DueDate
            });
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateTask(int id, UpdateTaskDto dto)
        {
            var userId = GetUserId();
            var task = new Models.TaskItem { Title = dto.Title, IsDone = dto.IsDone, Priority = dto.Priority, DueDate = dto.DueDate };
            var success = await _taskService.UpdateTaskAsync(id, task, userId);
            if (!success) return NotFound();
            return NoContent();
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteTask(int id)
        {
            var userId = GetUserId();
            var success = await _taskService.DeleteTaskAsync(id, userId);
            if (!success) return NotFound();
            return NoContent();
        }
    }
}