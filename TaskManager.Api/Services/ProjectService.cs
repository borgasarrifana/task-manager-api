using Microsoft.EntityFrameworkCore;
using TaskManager.Api.Common;
using TaskManager.Api.Data;
using TaskManager.Api.Models;

namespace TaskManager.Api.Services
{
    public class ProjectService : IProjectService
    {
        private const int MaxPageSize = 100;
        private readonly AppDbContext _context;

        public ProjectService(AppDbContext context)
        {
            _context = context;
        }

        public async Task<PagedResult<Project>> GetAllProjectsAsync(int userId, bool isAdmin = false, int page = 1, int pageSize = 10)
        {
            page = page < 1 ? 1 : page;
            pageSize = pageSize < 1 ? 1 : Math.Min(pageSize, MaxPageSize);

            var query = _context.Projects.Include(p => p.User).AsQueryable();
            if (!isAdmin)
            {
                query = query.Where(p => p.UserId == userId);
            }

            var totalCount = await query.CountAsync();
            var items = await query
                .OrderBy(p => p.Id)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return new PagedResult<Project>
            {
                Items = items,
                Page = page,
                PageSize = pageSize,
                TotalCount = totalCount
            };
        }

        public async Task<Project?> GetProjectByIdAsync(int id, int userId, bool isAdmin = false)
        {
            return await _context.Projects
                .Include(p => p.User)
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

        public async Task<bool> CompleteProjectAsync(int id, int userId, bool isAdmin = false)
        {
            var project = await _context.Projects
                .Include(p => p.Tasks)
                .FirstOrDefaultAsync(p => p.Id == id && (isAdmin || p.UserId == userId));
            if (project == null) return false;

            project.IsCompleted = true;
            foreach (var task in project.Tasks)
            {
                task.IsDone = true;
            }

            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> ReopenProjectAsync(int id, int userId, bool isAdmin = false)
        {
            var project = await _context.Projects
                .FirstOrDefaultAsync(p => p.Id == id && (isAdmin || p.UserId == userId));
            if (project == null) return false;

            project.IsCompleted = false;
            await _context.SaveChangesAsync();
            return true;
        }
    }
}