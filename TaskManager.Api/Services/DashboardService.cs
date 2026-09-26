using Microsoft.EntityFrameworkCore;
using TaskManager.Api.Data;
using TaskManager.Api.DTOs;
using TaskManager.Api.Models;

namespace TaskManager.Api.Services
{
    public class DashboardService : IDashboardService
    {
        private const int MaxProjects = 6;
        private const int MaxUpcoming = 5;

        private readonly AppDbContext _context;
        private readonly TimeProvider _time;

        public DashboardService(AppDbContext context, TimeProvider time)
        {
            _context = context;
            _time = time;
        }

        public async Task<DashboardDto> GetDashboardAsync(int userId, bool isAdmin = false)
        {
            // Start of today (UTC) — timestamptz columns require UTC DateTimes with Npgsql
            var today = _time.GetUtcNow().UtcDateTime.Date;
            var weekEnd = today.AddDays(7);

            var scopedProjects = _context.Projects
                .AsNoTracking()
                .Where(p => isAdmin || p.UserId == userId);

            // Query 1: per-project stats — one SQL statement with correlated COUNT subqueries
            var projectStats = await scopedProjects
                .Select(p => new
                {
                    p.Id,
                    p.Name,
                    p.IsCompleted,
                    OwnerUsername = p.User != null ? p.User.Username : null,
                    Total = p.Tasks.Count(),
                    Done = p.Tasks.Count(t => t.IsDone),
                    Overdue = p.Tasks.Count(t => !t.IsDone && t.DueDate != null && t.DueDate < today),
                    DueSoon = p.Tasks.Count(t => !t.IsDone && t.DueDate != null && t.DueDate >= today && t.DueDate < weekEnd)
                })
                .ToListAsync();

            var active = projectStats.Where(p => !p.IsCompleted).ToList();
            var totalTasks = active.Sum(p => p.Total);
            var doneTasks = active.Sum(p => p.Done);

            var totals = new DashboardTotalsDto
            {
                ActiveProjects = active.Count,
                CompletedProjects = projectStats.Count - active.Count,
                TotalTasks = totalTasks,
                DoneTasks = doneTasks,
                OpenTasks = totalTasks - doneTasks,
                OverdueTasks = active.Sum(p => p.Overdue),
                DueThisWeek = active.Sum(p => p.DueSoon),
                CompletionPercent = Percent(doneTasks, totalTasks)
            };

            // Most urgent active projects first: overdue, then most open work
            var projects = active
                .OrderByDescending(p => p.Overdue)
                .ThenByDescending(p => p.Total - p.Done)
                .ThenBy(p => p.Name)
                .Take(MaxProjects)
                .Select(p => new ProjectProgressDto
                {
                    Id = p.Id,
                    Name = p.Name,
                    OwnerUsername = p.OwnerUsername,
                    TotalTasks = p.Total,
                    DoneTasks = p.Done,
                    OverdueTasks = p.Overdue,
                    CompletionPercent = Percent(p.Done, p.Total)
                })
                .ToList();

            // Open tasks in active, in-scope projects (shared by queries 2 and 3)
            var openTasks =
                from t in _context.Tasks.AsNoTracking()
                join p in scopedProjects on t.ProjectId equals p.Id
                where !p.IsCompleted && !t.IsDone
                select new
                {
                    t.Id,
                    t.Title,
                    t.Priority,
                    t.DueDate,
                    t.ProjectId,
                    ProjectName = p.Name
                };

            // Query 2: open tasks by priority — SQL GROUP BY
            var priorityCounts = await openTasks
                .GroupBy(t => t.Priority)
                .Select(g => new { Priority = g.Key, Count = g.Count() })
                .ToListAsync();

            int CountFor(Priority priority) =>
                priorityCounts.FirstOrDefault(x => x.Priority == priority)?.Count ?? 0;

            // Query 3: next deadlines (overdue ones sort first — they're the most urgent)
            var upcomingRows = await openTasks
                .Where(t => t.DueDate != null)
                .OrderBy(t => t.DueDate)
                .ThenBy(t => t.Id)
                .Take(MaxUpcoming)
                .ToListAsync();

            return new DashboardDto
            {
                Totals = totals,
                OpenByPriority = new PriorityBreakdownDto
                {
                    High = CountFor(Priority.High),
                    Medium = CountFor(Priority.Medium),
                    Low = CountFor(Priority.Low)
                },
                Projects = projects,
                Upcoming = upcomingRows.Select(t => new UpcomingTaskDto
                {
                    Id = t.Id,
                    Title = t.Title,
                    ProjectId = t.ProjectId,
                    ProjectName = t.ProjectName,
                    DueDate = t.DueDate!.Value,
                    Priority = t.Priority,
                    IsOverdue = t.DueDate < today
                }).ToList()
            };
        }

        private static int Percent(int part, int total) =>
            total == 0 ? 0 : (int)Math.Round(part * 100.0 / total);
    }
}