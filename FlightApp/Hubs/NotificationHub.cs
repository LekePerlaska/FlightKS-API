using System.Security.Claims;
using FlightKS.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace FlightKS.Hubs;

[Authorize]
public class NotificationHub(IUserService userService) : Hub
{
    public const string NotificationReceived = "NotificationReceived";

    public override async Task OnConnectedAsync()
    {
        var keycloakId = Context.User?.FindFirstValue("sub");
        if (keycloakId is not null)
        {
            var user = await userService.GetByKeycloakIdAsync(keycloakId, Context.ConnectionAborted);
            if (user is not null)
                await Groups.AddToGroupAsync(Context.ConnectionId, $"user:{user.Id}", Context.ConnectionAborted);
        }
        await base.OnConnectedAsync();
    }
}
