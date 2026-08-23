using Microsoft.AspNetCore.Mvc;
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

        private static ProjectResponseDto ToDto(Project project) => new()
        {
            Id = project.Id,
            Name = project.Name
        };

        private static TaskResponseDto ToTaskDto(TaskItem task) => new()
        {
            Id = task.Id,
            Title = task.Title,
            IsDone = task.IsDone,
            CreatedAt = task.CreatedAt
        };

        [HttpGet]
        public async Task<ActionResult<IEnumerable<ProjectResponseDto>>> GetProjects()
        {
            var projects = await _projectService.GetAllProjectsAsync();
            return Ok(projects.Select(ToDto));
        }

        [HttpPost]
        public async Task<ActionResult<ProjectResponseDto>> CreateProject(CreateProjectDto dto)
        {
            var project = new Project { Name = dto.Name };
            var created = await _projectService.CreateProjectAsync(project);
            return CreatedAtAction(nameof(GetProjects), new { id = created.Id }, ToDto(created));
        }

        // Nested: list tasks belonging to a specific project
        [HttpGet("{projectId}/tasks")]
        public async Task<ActionResult<IEnumerable<TaskResponseDto>>> GetProjectTasks(int projectId)
        {
            var project = await _projectService.GetProjectByIdAsync(projectId);
            if (project == null) return NotFound($"Project {projectId} not found.");

            var tasks = await _taskService.GetTasksByProjectIdAsync(projectId);
            return Ok(tasks.Select(ToTaskDto));
        }

        // Nested: create a task under a specific project
        [HttpPost("{projectId}/tasks")]
        public async Task<ActionResult<TaskResponseDto>> CreateProjectTask(int projectId, CreateTaskDto dto)
        {
            var project = await _projectService.GetProjectByIdAsync(projectId);
            if (project == null) return NotFound($"Project {projectId} not found.");

            var task = new TaskItem { Title = dto.Title };
            var created = await _taskService.CreateTaskAsync(task, projectId);
            return CreatedAtAction(nameof(GetProjectTasks), new { projectId }, ToTaskDto(created));
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteProject(int id)
        {
            var success = await _projectService.DeleteProjectAsync(id);
            if (!success) return NotFound();
            return NoContent();
        }
    }
}