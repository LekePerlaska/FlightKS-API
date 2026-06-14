using System.Security.Claims;
using FlightKS.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace FlightKS.Hubs;

[Authorize]
public class NotificationHub(IUserService userService, ILogger<NotificationHub> logger) : Hub
{
    public const string NotificationReceived = "NotificationReceived";

    public override async Task OnConnectedAsync()
    {
        var keycloakId = Context.User?.FindFirstValue("sub");
        if (keycloakId is not null)
        {
            try
            {
                var user = await userService.GetByKeycloakIdAsync(keycloakId, Context.ConnectionAborted);
                if (user is not null)
                    await Groups.AddToGroupAsync(Context.ConnectionId, $"user:{user.Id}", Context.ConnectionAborted);
                else
                    logger.LogWarning("NotificationHub: no local user for Keycloak sub {Sub} — connection accepted but not added to notification group.", keycloakId);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                // Degrade gracefully: accept the connection without group membership rather than
                // failing the entire hub handshake (e.g. when the DB is temporarily unavailable).
                logger.LogError(ex, "NotificationHub: failed to resolve user for Keycloak sub {Sub}.", keycloakId);
            }
        }
        await base.OnConnectedAsync();
    }
}
