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

        group.MapPost("/", Create).WithName("CreateUser");
        group.MapGet("/me", GetMe).WithName("GetCurrentUserProfile").RequireAuthorization();
        group.MapPatch("/me", UpdateMe).WithName("UpdateCurrentUserProfile").RequireAuthorization();
        group.MapGet("/{id:guid}", GetById).WithName("GetUserById");
        group.MapPatch("/{id:guid}", Update).WithName("UpdateUser").RequireAuthorization();

        return app;
    }

    private static async Task<IResult> GetMe(
        ICurrentUserAccessor accessor,
        IUserService users,
        CancellationToken cancellationToken)
    {
        var user = await users.GetByKeycloakIdAsync(accessor.KeycloakUserId, cancellationToken);
        return user is null ? TypedResults.NotFound() : TypedResults.Ok(user.ToResponse());
    }

    private static async Task<IResult> UpdateMe(
        UserUpdateDto dto,
        ICurrentUserAccessor accessor,
        IUserService users,
        CancellationToken cancellationToken)
    {
        var user = await users.GetByKeycloakIdAsync(accessor.KeycloakUserId, cancellationToken);
        if (user is null) return TypedResults.NotFound();

        var updated = await users.UpdateAsync(
            user.Id, dto.FullName, dto.PhoneNumber, dto.DateOfBirth,
            dto.PassportNumber, dto.Nationality, cancellationToken);

        return updated is null ? TypedResults.NotFound() : TypedResults.Ok(updated.ToResponse());
    }

    private static async Task<IResult> Create(
        UserCreateDto dto,
        IKeycloakService keycloak,
        IUserService users,
        CancellationToken cancellationToken)
    {
        string keycloakUserId;
        try
        {
            keycloakUserId = await keycloak.CreateUserAsync(
                dto.Email, dto.FullName, dto.Password, cancellationToken);
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("already exists"))
        {
            return TypedResults.Conflict(new { error = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return TypedResults.BadRequest(new { error = ex.Message });
        }

        try
        {
            var user = await users.CreateAsync(
                keycloakUserId, dto.FullName, dto.Email,
                dto.PhoneNumber, dto.DateOfBirth, dto.PassportNumber, dto.Nationality,
                cancellationToken);

            return TypedResults.Created($"/api/v1/users/{user.Id}", user.ToResponse());
        }
        catch (InvalidOperationException ex)
        {
            return TypedResults.Conflict(new { error = ex.Message });
        }
    }

    private static async Task<IResult> GetById(
        Guid id,
        IUserService users,
        CancellationToken cancellationToken)
    {
        var user = await users.GetByIdAsync(id, cancellationToken);
        return user is null ? TypedResults.NotFound() : TypedResults.Ok(user.ToResponse());
    }

    private static async Task<IResult> Update(
        Guid id,
        UserUpdateDto dto,
        IUserService users,
        CancellationToken cancellationToken)
    {
        var updated = await users.UpdateAsync(
            id, dto.FullName, dto.PhoneNumber, dto.DateOfBirth,
            dto.PassportNumber, dto.Nationality, cancellationToken);

        return updated is null ? TypedResults.NotFound() : TypedResults.Ok(updated.ToResponse());
    }
}
