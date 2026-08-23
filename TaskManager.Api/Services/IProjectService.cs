using TaskManager.Api.Models;

namespace TaskManager.Api.Services
{
    public interface IProjectService
    {
        Task<IEnumerable<Project>> GetAllProjectsAsync(int userId);
        Task<Project?> GetProjectByIdAsync(int id, int userId);
        Task<Project> CreateProjectAsync(Project project);
        Task<bool> DeleteProjectAsync(int id, int userId);
    }
}