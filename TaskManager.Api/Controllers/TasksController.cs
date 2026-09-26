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
        private readonly IProjectService _projectService;
        private readonly IRealtimeNotifier _notifier;

        public TasksController(ITaskService taskService, IProjectService projectService, IRealtimeNotifier notifier)
        {
            _taskService = taskService;
            _projectService = projectService;
            _notifier = notifier;
        }

        private int GetUserId()
        {
            var idClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            return int.Parse(idClaim!);
        }

        private bool IsAdmin() => User.IsInRole("Admin");

        [HttpGet("{id}")]
        public async Task<ActionResult<TaskResponseDto>> GetTask(int id)
        {
            var userId = GetUserId();
            var task = await _taskService.GetTaskByIdAsync(id, userId, IsAdmin());
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
            var existing = await _taskService.GetTaskByIdAsync(id, userId, IsAdmin());
            if (existing == null) return NotFound();
            var projectId = existing.ProjectId;

            var task = new Models.TaskItem { Title = dto.Title, IsDone = dto.IsDone, Priority = dto.Priority, DueDate = dto.DueDate };
            var result = await _taskService.UpdateTaskAsync(id, task, userId, IsAdmin());

            if (result == TaskOperationResult.Success)
            {
                await NotifyTaskChangedAsync(projectId, id, "updated", userId);
            }

            return result switch
            {
                TaskOperationResult.Success => NoContent(),
                TaskOperationResult.ProjectCompleted => BadRequest("Cannot modify tasks in a completed project."),
                _ => NotFound()
            };
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteTask(int id)
        {
            var userId = GetUserId();
            var existing = await _taskService.GetTaskByIdAsync(id, userId, IsAdmin());
            if (existing == null) return NotFound();
            var projectId = existing.ProjectId;

            var result = await _taskService.DeleteTaskAsync(id, userId, IsAdmin());

            if (result == TaskOperationResult.Success)
            {
                await NotifyTaskChangedAsync(projectId, id, "deleted", userId);
            }

            return result switch
            {
                TaskOperationResult.Success => NoContent(),
                TaskOperationResult.ProjectCompleted => BadRequest("Cannot modify tasks in a completed project."),
                _ => NotFound()
            };
        }

        private async Task NotifyTaskChangedAsync(int projectId, int taskId, string change, int userId)
        {
            var project = await _projectService.GetProjectByIdAsync(projectId, userId, IsAdmin());
            if (project != null)
            {
                await _notifier.TaskChangedAsync(projectId, project.UserId, taskId, change);
            }
        }
    }
}