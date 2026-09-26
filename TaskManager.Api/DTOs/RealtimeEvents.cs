namespace TaskManager.Api.DTOs
{
    // Change values: "created" | "updated" | "deleted" | "completed" | "reopened"
    public record TaskChangedEvent(int ProjectId, int TaskId, string Change);
    public record ProjectChangedEvent(int ProjectId, string Change);
}