using FlightKS.Auth;
using FlightKS.Mappers;
using FlightKS.Models.Dtos.Auth;
using FlightKS.Services.Interfaces;

namespace FlightKS.Endpoints.V1;

public static class AuthEndpoints
{
    public static IEndpointRouteBuilder MapAuthEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/auth").WithTags("Auth").RequireAuthorization();

        group.MapGet("/me", Me).WithName("AuthMe");
        group.MapPost("/logout", Logout).WithName("AuthLogout");

        return app;
    }

    private static async Task<IResult> Me(ICurrentUserAccessor current, IUserService users, CancellationToken cancellationToken)
    {
        var kid = current.KeycloakUserId;
        if (string.IsNullOrEmpty(kid)) return TypedResults.Unauthorized();
        var user = await users.GetByKeycloakIdAsync(kid, cancellationToken);
        return TypedResults.Ok(new AuthMeResponseDto(kid, user?.ToResponse()));
    }

    private static IResult Logout() => TypedResults.NoContent();
}
