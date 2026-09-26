using TaskManager.Api.DTOs;

namespace TaskManager.Api.Services
{
    public interface IDashboardService
    {
        Task<DashboardDto> GetDashboardAsync(int userId, bool isAdmin = false);
    }
}