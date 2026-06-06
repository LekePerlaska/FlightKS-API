using FlightKS.Auth;
using FlightKS.Endpoints;
using FlightKS.Mappers;
using FlightKS.Models.Dtos.Auth;
using FlightKS.Services.Interfaces;

namespace FlightKS.Endpoints.V1;

public static class AuthEndpoints
{
    public static IEndpointRouteBuilder MapAuthEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/auth").WithTags("Auth");

        group.MapGet("/me", GetMe)
            .RequireAuthorization()
            .WithName("GetCurrentUser");

        group.MapPost("/logout", Logout)
            .RequireAuthorization()
            .WithName("Logout")
            .WithValidation<LogoutDto>();

        return app;
    }

    private static async Task<IResult> GetMe(
        ICurrentUserAccessor currentUser,
        IUserService users,
        CancellationToken cancellationToken)
    {
        var user = await users.GetOrCreateAsync(
            currentUser.KeycloakUserId,
            currentUser.Email,
            currentUser.FullName,
            cancellationToken);
        return TypedResults.Ok(user.ToResponse(currentUser.Roles));
    }

    private static async Task<IResult> Logout(
        LogoutDto dto,
        IKeycloakService keycloak,
        CancellationToken cancellationToken)
    {
        await keycloak.LogoutAsync(dto.RefreshToken, cancellationToken);
        return TypedResults.NoContent();
    }
}
