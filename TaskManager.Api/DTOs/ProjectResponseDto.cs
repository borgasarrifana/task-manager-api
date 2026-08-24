namespace TaskManager.Api.DTOs
{
    public class ProjectResponseDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? OwnerUsername { get; set; }
    }
}
