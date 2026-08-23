namespace TaskManager.Api.Models
{
    public class TaskItem
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public bool IsDone { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public Priority Priority { get; set; } = Priority.Medium;
        public DateTime? DueDate { get; set; }

        public int ProjectId { get; set; }
        public Project? Project { get; set; }
    }
}