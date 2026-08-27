using TaskManager.Api.Common;
using TaskManager.Api.Models;

namespace TaskManager.Api.Services
{
    public enum TaskOperationResult
    {
        Success,
        NotFound,
        ProjectCompleted
    }

    public interface ITaskService
    {
        Task<TaskItem?> GetTaskByIdAsync(int id, int userId, bool isAdmin = false);
        Task<PagedResult<TaskItem>> GetTasksByProjectIdAsync(int projectId, int page = 1, int pageSize = 20, Priority? priority = null, string? sortBy = null);
        Task<TaskItem> CreateTaskAsync(TaskItem task, int projectId);
        Task<TaskOperationResult> UpdateTaskAsync(int id, TaskItem updatedTask, int userId, bool isAdmin = false);
        Task<TaskOperationResult> DeleteTaskAsync(int id, int userId, bool isAdmin = false);
    }
}