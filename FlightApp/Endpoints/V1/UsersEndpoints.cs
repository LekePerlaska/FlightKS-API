using FlightKS.Auth;
using FlightKS.Mappers;
using FlightKS.Models.Dtos.Users;
using FlightKS.Services.Interfaces;

namespace FlightKS.Endpoints.V1;

public static class UsersEndpoints
{
    public static IEndpointRouteBuilder MapUsersEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/users").WithTags("Users");

        group.MapPost("/", Create).WithName("CreateUser").RequireAuthorization();
        group.MapGet("/me", Me).WithName("GetMe").RequireAuthorization();
        group.MapPatch("/me", UpdateMe).WithName("UpdateMe").RequireAuthorization();

        return app;
    }

    private static async Task<IResult> Create(UserCreateDto dto, ICurrentUserAccessor current, IUserService users, CancellationToken cancellationToken)
    {
        var kid = current.KeycloakUserId;
        if (string.IsNullOrEmpty(kid)) return TypedResults.Unauthorized();

        try
        {
            var user = await users.CreateAsync(
                kid, dto.FullName, dto.Email, dto.PhoneNumber, dto.DateOfBirth,
                dto.PassportNumber, dto.Nationality, cancellationToken);
            return TypedResults.Created($"/api/v1/users/{user.Id}", user.ToResponse());
        }
        catch (InvalidOperationException ex)
        {
            return TypedResults.Conflict(new { error = ex.Message });
        }
    }

    private static async Task<IResult> Me(ICurrentUserAccessor current, IUserService users, CancellationToken cancellationToken)
    {
        var kid = current.KeycloakUserId;
        if (string.IsNullOrEmpty(kid)) return TypedResults.Unauthorized();
        var user = await users.GetByKeycloakIdAsync(kid, cancellationToken);
        return user is null ? TypedResults.NotFound() : TypedResults.Ok(user.ToResponse());
    }

    private static async Task<IResult> UpdateMe(UserUpdateDto dto, ICurrentUserAccessor current, IUserService users, CancellationToken cancellationToken)
    {
        var userId = await current.GetUserIdAsync(cancellationToken);
        if (userId is null) return TypedResults.NotFound();
        var updated = await users.UpdateAsync(
            userId.Value, dto.FullName, dto.PhoneNumber, dto.DateOfBirth,
            dto.PassportNumber, dto.Nationality, cancellationToken);
        return updated is null ? TypedResults.NotFound() : TypedResults.Ok(updated.ToResponse());
    }
}
