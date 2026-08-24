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
        Task<IEnumerable<TaskItem>> GetTasksByProjectIdAsync(int projectId);
        Task<TaskItem> CreateTaskAsync(TaskItem task, int projectId);
        Task<TaskOperationResult> UpdateTaskAsync(int id, TaskItem updatedTask, int userId, bool isAdmin = false);
        Task<TaskOperationResult> DeleteTaskAsync(int id, int userId, bool isAdmin = false);
    }
}
