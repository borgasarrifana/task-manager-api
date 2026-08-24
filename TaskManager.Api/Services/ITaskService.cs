using TaskManager.Api.Models;

namespace TaskManager.Api.Services
{
    public interface ITaskService
    {
        Task<TaskItem?> GetTaskByIdAsync(int id, int userId, bool isAdmin = false);
        Task<IEnumerable<TaskItem>> GetTasksByProjectIdAsync(int projectId);
        Task<TaskItem> CreateTaskAsync(TaskItem task, int projectId);
        Task<bool> UpdateTaskAsync(int id, TaskItem updatedTask, int userId, bool isAdmin = false);
        Task<bool> DeleteTaskAsync(int id, int userId, bool isAdmin = false);
    }
}