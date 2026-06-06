using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace FlightKS.Hubs;

[Authorize]
public class SeatHub : Hub
{
    public async Task JoinScheduleGroup(string scheduleId)
        => await Groups.AddToGroupAsync(Context.ConnectionId, scheduleId);

    public async Task LeaveScheduleGroup(string scheduleId)
        => await Groups.RemoveFromGroupAsync(Context.ConnectionId, scheduleId);
}
