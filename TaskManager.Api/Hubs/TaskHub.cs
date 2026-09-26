using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using System.Security.Claims;
using TaskManager.Api.Services;

namespace TaskManager.Api.Hubs
{
    [Authorize]
    public class TaskHub : Hub
    {
        private readonly IProjectService _projectService;

        public TaskHub(IProjectService projectService)
        {
            _projectService = projectService;
        }

        private int GetUserId() =>
            int.Parse(Context.User!.FindFirst(ClaimTypes.NameIdentifier)!.Value);

        private bool IsAdmin() => Context.User?.IsInRole("Admin") ?? false;

        public override async Task OnConnectedAsync()
        {
            // Per-user delivery works via Clients.User(id) — SignalR maps users by the
            // NameIdentifier claim — so only admins need an explicit group
            if (IsAdmin())
            {
                await Groups.AddToGroupAsync(Context.ConnectionId, RealtimeGroups.Admins);
            }
            await base.OnConnectedAsync();
        }

        // Called by a client when it opens a project's task list.
        // Access is checked with the same rule as the REST API.
        public async Task<bool> JoinProject(int projectId)
        {
            var project = await _projectService.GetProjectByIdAsync(projectId, GetUserId(), IsAdmin());
            if (project == null) return false;

            await Groups.AddToGroupAsync(Context.ConnectionId, RealtimeGroups.Project(projectId));
            return true;
        }

        public Task LeaveProject(int projectId) =>
            Groups.RemoveFromGroupAsync(Context.ConnectionId, RealtimeGroups.Project(projectId));
    }
}