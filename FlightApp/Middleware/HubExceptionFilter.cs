using FlightKS.Exceptions;
using Microsoft.AspNetCore.SignalR;

namespace FlightKS.Middleware;

public sealed class HubExceptionFilter : IHubFilter
{
    public async ValueTask<object?> InvokeMethodAsync(
        HubInvocationContext context,
        Func<HubInvocationContext, ValueTask<object?>> next)
    {
        try
        {
            return await next(context);
        }
        catch (AppException ex)
        {
            throw new HubException($"{ex.Code}: {ex.Message}");
        }
    }
}
