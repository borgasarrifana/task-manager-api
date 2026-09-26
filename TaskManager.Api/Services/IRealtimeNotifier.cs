namespace TaskManager.Api.Services
{
    public interface IRealtimeNotifier
    {
        Task TaskChangedAsync(int projectId, int ownerId, int taskId, string change);
        Task ProjectChangedAsync(int projectId, int ownerId, string change);
    }
}