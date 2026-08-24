using TaskManager.Api.Models;

namespace TaskManager.Api.Services
{
    public interface IProjectService
    {
        Task<IEnumerable<Project>> GetAllProjectsAsync(int userId, bool isAdmin = false);
        Task<Project?> GetProjectByIdAsync(int id, int userId, bool isAdmin = false);
        Task<Project> CreateProjectAsync(Project project);
        Task<bool> DeleteProjectAsync(int id, int userId, bool isAdmin = false);
    }
}