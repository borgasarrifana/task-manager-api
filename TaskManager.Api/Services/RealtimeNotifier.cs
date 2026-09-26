using Microsoft.AspNetCore.SignalR;
using TaskManager.Api.DTOs;
using TaskManager.Api.Hubs;

namespace TaskManager.Api.Services
{
    public class RealtimeNotifier : IRealtimeNotifier
    {
        private readonly IHubContext<TaskHub> _hub;
        private readonly ILogger<RealtimeNotifier> _logger;

        public RealtimeNotifier(IHubContext<TaskHub> hub, ILogger<RealtimeNotifier> logger)
        {
            _hub = hub;
            _logger = logger;
        }

        public Task TaskChangedAsync(int projectId, int ownerId, int taskId, string change) =>
            BroadcastAsync("TaskChanged", new TaskChangedEvent(projectId, taskId, change), projectId, ownerId);

        public Task ProjectChangedAsync(int projectId, int ownerId, string change) =>
            BroadcastAsync("ProjectChanged", new ProjectChangedEvent(projectId, change), projectId, ownerId);

        // Viewers of the project + its owner + all admins.
        // A connection in more than one audience may get the event twice;
        // clients debounce their refetch, so that's harmless.
        private async Task BroadcastAsync(string method, object payload, int projectId, int ownerId)
        {
            try
            {
                await Task.WhenAll(
                    _hub.Clients.Group(RealtimeGroups.Project(projectId)).SendAsync(method, payload),
                    _hub.Clients.User(ownerId.ToString()).SendAsync(method, payload),
                    _hub.Clients.Group(RealtimeGroups.Admins).SendAsync(method, payload)
                );
            }
            catch (Exception ex)
            {
                // The write already succeeded — a failed broadcast must not turn it into a 500
                _logger.LogWarning(ex, "Realtime broadcast {Method} failed for project {ProjectId}", method, projectId);
            }
        }
    }
}