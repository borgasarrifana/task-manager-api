using Microsoft.EntityFrameworkCore;
using TaskManager.Api.Data;
using TaskManager.Api.Models;

namespace TaskManager.Api.Services
{
    public class ProjectService : IProjectService
    {
        private readonly AppDbContext _context;

        public ProjectService(AppDbContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<Project>> GetAllProjectsAsync(int userId, bool isAdmin = false)
        {
            var query = _context.Projects.AsQueryable();
            if (!isAdmin)
            {
                query = query.Where(p => p.UserId == userId);
            }
            return await query.ToListAsync();
        }

        public async Task<Project?> GetProjectByIdAsync(int id, int userId, bool isAdmin = false)
        {
            return await _context.Projects
                .FirstOrDefaultAsync(p => p.Id == id && (isAdmin || p.UserId == userId));
        }

        public async Task<Project> CreateProjectAsync(Project project)
        {
            _context.Projects.Add(project);
            await _context.SaveChangesAsync();
            return project;
        }

        public async Task<bool> DeleteProjectAsync(int id, int userId, bool isAdmin = false)
        {
            var project = await _context.Projects
                .FirstOrDefaultAsync(p => p.Id == id && (isAdmin || p.UserId == userId));
            if (project == null) return false;

            _context.Projects.Remove(project);
            await _context.SaveChangesAsync();
            return true;
        }
    }
}