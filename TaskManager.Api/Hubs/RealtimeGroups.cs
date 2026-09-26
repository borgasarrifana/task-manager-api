namespace TaskManager.Api.Hubs
{
    public static class RealtimeGroups
    {
        public const string Admins = "admins";
        public static string Project(int projectId) => $"project-{projectId}";
    }
}