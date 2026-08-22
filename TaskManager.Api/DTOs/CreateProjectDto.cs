using System.ComponentModel.DataAnnotations;

namespace TaskManager.Api.DTOs
{
    public class CreateProjectDto
    {
        [Required]
        [StringLength(100, MinimumLength = 1)]
        public string Name { get; set; } = string.Empty;
    }
}