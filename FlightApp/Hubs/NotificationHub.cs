using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace FlightKS.Hubs;

[Authorize]
public class NotificationHub : Hub
{
    public const string NotificationReceived = "NotificationReceived";
}
