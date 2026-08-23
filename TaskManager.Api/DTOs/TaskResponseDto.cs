using TaskManager.Api.Models;

namespace TaskManager.Api.DTOs
{
    public class TaskResponseDto
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public bool IsDone { get; set; }
        public DateTime CreatedAt { get; set; }
        public Priority Priority { get; set; }
        public DateTime? DueDate { get; set; }
    }
}