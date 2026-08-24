using Microsoft.EntityFrameworkCore;
using TaskManager.Api.Data;
using TaskManager.Api.Models;

namespace TaskManager.Api.Services
{
    public class TaskService : ITaskService
    {
        private readonly AppDbContext _context;

        public TaskService(AppDbContext context)
        {
            _context = context;
        }

        public async Task<TaskItem?> GetTaskByIdAsync(int id, int userId, bool isAdmin = false)
        {
            return await _context.Tasks
                .Include(t => t.Project)
                .FirstOrDefaultAsync(t => t.Id == id && (isAdmin || t.Project!.UserId == userId));
        }

        public async Task<IEnumerable<TaskItem>> GetTasksByProjectIdAsync(int projectId)
        {
            return await _context.Tasks.Where(t => t.ProjectId == projectId).ToListAsync();
        }

        public async Task<TaskItem> CreateTaskAsync(TaskItem task, int projectId)
        {
            task.ProjectId = projectId;
            if (task.DueDate.HasValue)
            {
                task.DueDate = DateTime.SpecifyKind(task.DueDate.Value, DateTimeKind.Utc);
            }
            _context.Tasks.Add(task);
            await _context.SaveChangesAsync();
            return task;
        }

        public async Task<TaskOperationResult> UpdateTaskAsync(int id, TaskItem updatedTask, int userId, bool isAdmin = false)
        {
            var task = await _context.Tasks
                .Include(t => t.Project)
                .FirstOrDefaultAsync(t => t.Id == id && (isAdmin || t.Project!.UserId == userId));
            if (task == null) return TaskOperationResult.NotFound;
            if (task.Project!.IsCompleted) return TaskOperationResult.ProjectCompleted;

            task.Title = updatedTask.Title;
            task.IsDone = updatedTask.IsDone;
            task.Priority = updatedTask.Priority;
            task.DueDate = updatedTask.DueDate.HasValue
                ? DateTime.SpecifyKind(updatedTask.DueDate.Value, DateTimeKind.Utc)
                : null;
            await _context.SaveChangesAsync();
            return TaskOperationResult.Success;
        }

        public async Task<TaskOperationResult> DeleteTaskAsync(int id, int userId, bool isAdmin = false)
        {
            var task = await _context.Tasks
                .Include(t => t.Project)
                .FirstOrDefaultAsync(t => t.Id == id && (isAdmin || t.Project!.UserId == userId));
            if (task == null) return TaskOperationResult.NotFound;
            if (task.Project!.IsCompleted) return TaskOperationResult.ProjectCompleted;

            _context.Tasks.Remove(task);
            await _context.SaveChangesAsync();
            return TaskOperationResult.Success;
        }
    }
}
