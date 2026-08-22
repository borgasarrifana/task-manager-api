namespace TaskManager.Api.Models
{
    public class Project
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;

        // Navigation property: one Project has many Tasks
        public List<TaskItem> Tasks { get; set; } = new();
    }
}