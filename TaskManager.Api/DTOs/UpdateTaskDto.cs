using System.ComponentModel.DataAnnotations;
using TaskManager.Api.Models;

namespace TaskManager.Api.DTOs
{
    public class UpdateTaskDto
    {
        [Required]
        [StringLength(200, MinimumLength = 1)]
        public string Title { get; set; } = string.Empty;
        public bool IsDone { get; set; }
        public Priority Priority { get; set; } = Priority.Medium;
        public DateTime? DueDate { get; set; }
    }
}