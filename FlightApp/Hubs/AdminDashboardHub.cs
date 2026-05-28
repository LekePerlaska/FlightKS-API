using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace FlightKS.Hubs;

[Authorize(Roles = "Admin")]
public class AdminDashboardHub : Hub
{
    public const string DashboardUpdated = "DashboardUpdated";
}
