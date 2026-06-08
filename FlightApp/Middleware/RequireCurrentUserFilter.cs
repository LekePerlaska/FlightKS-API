using FlightKS.Auth;

namespace FlightKS.Middleware;

public sealed class RequireCurrentUserFilter(ICurrentUserAccessor accessor) : IEndpointFilter
{
    internal const string ItemKey = "CurrentUserId";

    public async ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext ctx, EndpointFilterDelegate next)
    {
        var userId = await accessor.GetUserIdAsync(ctx.HttpContext.RequestAborted);
        if (userId is null) return TypedResults.Unauthorized();
        ctx.HttpContext.Items[ItemKey] = userId.Value;
        return await next(ctx);
    }
}

public static class HttpContextExtensions
{
    public static Guid CurrentUserId(this HttpContext ctx) =>
        (Guid)ctx.Items[RequireCurrentUserFilter.ItemKey]!;
}

public static class RouteGroupBuilderExtensions
{
    public static RouteGroupBuilder RequireCurrentUser(this RouteGroupBuilder group) =>
        group.AddEndpointFilter<RequireCurrentUserFilter>();
}
