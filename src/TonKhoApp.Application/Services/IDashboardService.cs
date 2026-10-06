using TonKhoApp.Application.Common;
using TonKhoApp.Application.DTOs;

namespace TonKhoApp.Application.Services
{
    public interface IDashboardService
    {
        Task<SingleResponse<DashboardData>> GetDashboardAsync(CancellationToken cancellationToken = default);
    }
}
