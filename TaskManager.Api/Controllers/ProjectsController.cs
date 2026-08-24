using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using TaskManager.Api.DTOs;
using TaskManager.Api.Models;
using TaskManager.Api.Services;
using Microsoft.AspNetCore.Authorization;

namespace TaskManager.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class ProjectsController : ControllerBase
    {
        private readonly IProjectService _projectService;
        private readonly ITaskService _taskService;

        public ProjectsController(IProjectService projectService, ITaskService taskService)
        {
            _projectService = projectService;
            _taskService = taskService;
        }

        private int GetUserId()
        {
            var idClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            return int.Parse(idClaim!);
        }

        private bool IsAdmin() => User.IsInRole("Admin");

        private static ProjectResponseDto ToDto(Project project) => new()
        {
            Id = project.Id,
            Name = project.Name,
            OwnerUsername = project.User?.Username,
            IsCompleted = project.IsCompleted
        };

        private static TaskResponseDto ToTaskDto(TaskItem task) => new()
        {
            Id = task.Id,
            Title = task.Title,
            IsDone = task.IsDone,
            CreatedAt = task.CreatedAt,
            Priority = task.Priority,
            DueDate = task.DueDate
        };

        [HttpGet]
        public async Task<ActionResult<IEnumerable<ProjectResponseDto>>> GetProjects()
        {
            var userId = GetUserId();
            var projects = await _projectService.GetAllProjectsAsync(userId, IsAdmin());
            return Ok(projects.Select(ToDto));
        }

        [HttpPost]
        public async Task<ActionResult<ProjectResponseDto>> CreateProject(CreateProjectDto dto)
        {
            var userId = GetUserId();
            var project = new Project { Name = dto.Name, UserId = userId };
            var created = await _projectService.CreateProjectAsync(project);
            return CreatedAtAction(nameof(GetProjects), new { id = created.Id }, ToDto(created));
        }

        [HttpGet("{projectId}/tasks")]
        public async Task<ActionResult<IEnumerable<TaskResponseDto>>> GetProjectTasks(int projectId)
        {
            var userId = GetUserId();
            var project = await _projectService.GetProjectByIdAsync(projectId, userId, IsAdmin());
            if (project == null) return NotFound($"Project {projectId} not found.");

            var tasks = await _taskService.GetTasksByProjectIdAsync(projectId);
            return Ok(tasks.Select(ToTaskDto));
        }

        [HttpPost("{projectId}/tasks")]
        public async Task<ActionResult<TaskResponseDto>> CreateProjectTask(int projectId, CreateTaskDto dto)
        {
            var userId = GetUserId();
            var project = await _projectService.GetProjectByIdAsync(projectId, userId, IsAdmin());
            if (project == null) return NotFound($"Project {projectId} not found.");
            if (project.IsCompleted) return BadRequest("Cannot add tasks to a completed project.");

            var task = new TaskItem { Title = dto.Title, Priority = dto.Priority, DueDate = dto.DueDate };
            var created = await _taskService.CreateTaskAsync(task, projectId);
            return CreatedAtAction(nameof(GetProjectTasks), new { projectId }, ToTaskDto(created));
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteProject(int id)
        {
            var userId = GetUserId();
            var success = await _projectService.DeleteProjectAsync(id, userId, IsAdmin());
            if (!success) return NotFound();
            return NoContent();
        }

        [HttpPut("{id}/complete")]
        public async Task<IActionResult> CompleteProject(int id)
        {
            var userId = GetUserId();
            var success = await _projectService.CompleteProjectAsync(id, userId, IsAdmin());
            if (!success) return NotFound();
            return NoContent();
        }

        [HttpPut("{id}/reopen")]
        public async Task<IActionResult> ReopenProject(int id)
        {
            var userId = GetUserId();
            var success = await _projectService.ReopenProjectAsync(id, userId, IsAdmin());
            if (!success) return NotFound();
            return NoContent();
        }
    }
}
