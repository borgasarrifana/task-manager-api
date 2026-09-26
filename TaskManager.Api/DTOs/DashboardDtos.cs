using TaskManager.Api.Models;

namespace TaskManager.Api.DTOs
{
    public class DashboardDto
    {
        public DashboardTotalsDto Totals { get; set; } = new();
        public PriorityBreakdownDto OpenByPriority { get; set; } = new();
        public List<ProjectProgressDto> Projects { get; set; } = new();
        public List<UpcomingTaskDto> Upcoming { get; set; } = new();
    }

    public class DashboardTotalsDto
    {
        public int ActiveProjects { get; set; }
        public int CompletedProjects { get; set; }
        public int TotalTasks { get; set; }      // tasks in active projects
        public int DoneTasks { get; set; }
        public int OpenTasks { get; set; }
        public int OverdueTasks { get; set; }
        public int DueThisWeek { get; set; }     // open, due today through the next 6 days
        public int CompletionPercent { get; set; }
    }

    public class PriorityBreakdownDto
    {
        public int High { get; set; }
        public int Medium { get; set; }
        public int Low { get; set; }
    }

    public class ProjectProgressDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? OwnerUsername { get; set; }
        public int TotalTasks { get; set; }
        public int DoneTasks { get; set; }
        public int OverdueTasks { get; set; }
        public int CompletionPercent { get; set; }
    }

    public class UpcomingTaskDto
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public int ProjectId { get; set; }
        public string ProjectName { get; set; } = string.Empty;
        public DateTime DueDate { get; set; }
        public Priority Priority { get; set; }
        public bool IsOverdue { get; set; }
    }
}