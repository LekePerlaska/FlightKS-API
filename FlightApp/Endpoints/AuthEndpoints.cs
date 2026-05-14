using FlightKS.Models.Dtos.Auth;
using FlightKS.Services.Interfaces;

namespace FlightKS.Endpoints;

public static class AuthEndpoints
{
    private const string RoutePrefix = "/auth";

    public static IEndpointRouteBuilder MapAuthEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup(RoutePrefix).WithTags("Auth");

        group.MapPost("/sign-up", SignUp).WithName("SignUp");
        group.MapPost("/sign-in", SignIn).WithName("SignIn");

        return app;
    }

    private static async Task<IResult> SignUp(SignUpRequestDto dto, IAuthService auth, CancellationToken cancellationToken)
    {
        try
        {
            var result = await auth.SignUpAsync(dto, cancellationToken);
            return TypedResults.Created($"/users/{result.User.Id}", result);
        }
        catch (InvalidOperationException ex)
        {
            return TypedResults.Conflict(new { error = ex.Message });
        }
    }

    private static async Task<IResult> SignIn(SignInRequestDto dto, IAuthService auth, CancellationToken cancellationToken)
    {
        var result = await auth.SignInAsync(dto, cancellationToken);
        return result is null
            ? TypedResults.Unauthorized()
            : TypedResults.Ok(result);
    }
}
