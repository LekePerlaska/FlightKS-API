using FlightKS.Models.Dtos.Users;
using FlightKS.Services.Interfaces;

namespace FlightKS.Endpoints;

public static class UserEndpoints
{
    private const string RoutePrefix = "/api/users";

    public static IEndpointRouteBuilder MapUserEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup(RoutePrefix).WithTags("Users");

        group.MapGet("/", GetAll).WithName("GetUsers");
        group.MapGet("/{id:guid}", GetById).WithName("GetUserById");
        group.MapPost("/", Create).WithName("CreateUser");
        group.MapPut("/{id:guid}", Update).WithName("UpdateUser");
        group.MapDelete("/{id:guid}", Delete).WithName("DeleteUser");

        return app;
    }

    private static async Task<IResult> GetAll(IUserService users, CancellationToken cancellationToken)
    {
        var result = await users.GetAllAsync(cancellationToken);
        return TypedResults.Ok(result);
    }

    private static async Task<IResult> GetById(Guid id, IUserService users, CancellationToken cancellationToken)
    {
        var user = await users.GetByIdAsync(id, cancellationToken);
        return user is null ? TypedResults.NotFound() : TypedResults.Ok(user);
    }

    private static async Task<IResult> Create(UserCreateDto dto, IUserService users, CancellationToken cancellationToken)
    {
        try
        {
            var created = await users.CreateAsync(dto, cancellationToken);
            return TypedResults.Created($"{RoutePrefix}/{created.Id}", created);
        }
        catch (InvalidOperationException ex)
        {
            return TypedResults.Conflict(new { error = ex.Message });
        }
    }

    private static async Task<IResult> Update(Guid id, UserUpdateDto dto, IUserService users, CancellationToken cancellationToken)
    {
        var updated = await users.UpdateAsync(id, dto, cancellationToken);
        return updated is null ? TypedResults.NotFound() : TypedResults.Ok(updated);
    }

    private static async Task<IResult> Delete(Guid id, IUserService users, CancellationToken cancellationToken)
    {
        var deleted = await users.DeleteAsync(id, cancellationToken);
        return deleted ? TypedResults.NoContent() : TypedResults.NotFound();
    }
}
