using TaskManager.Api.Common;
using TaskManager.Api.Models;

namespace TaskManager.Api.Services
{
    public interface IProjectService
    {
        Task<PagedResult<Project>> GetAllProjectsAsync(int userId, bool isAdmin = false, int page = 1, int pageSize = 20);
        Task<Project?> GetProjectByIdAsync(int id, int userId, bool isAdmin = false);
        Task<Project> CreateProjectAsync(Project project);
        Task<bool> DeleteProjectAsync(int id, int userId, bool isAdmin = false);
        Task<bool> CompleteProjectAsync(int id, int userId, bool isAdmin = false);
        Task<bool> ReopenProjectAsync(int id, int userId, bool isAdmin = false);
    }
}