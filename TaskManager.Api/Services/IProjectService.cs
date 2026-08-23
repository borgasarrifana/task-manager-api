using TaskManager.Api.Models;

namespace TaskManager.Api.Services
{
    public interface IProjectService
    {
        Task<IEnumerable<Project>> GetAllProjectsAsync();
        Task<Project?> GetProjectByIdAsync(int id);
        Task<Project> CreateProjectAsync(Project project);
        Task<bool> DeleteProjectAsync(int id);
    }
}