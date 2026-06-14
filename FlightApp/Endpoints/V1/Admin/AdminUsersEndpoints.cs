using FlightKS.Auth;
using FlightKS.Endpoints;
using FlightKS.Models.Dtos;
using FlightKS.Models.Dtos.Admin;
using FlightKS.Services.Interfaces;

namespace FlightKS.Endpoints.V1.Admin;

public static class AdminUsersEndpoints
{
    public static IEndpointRouteBuilder MapAdminUsersEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/admin/users").WithTags("AdminUsers").RequireAuthorization(Policies.Admin);
        var rolesGroup = app.MapGroup("/admin/roles").WithTags("AdminUsers").RequireAuthorization(Policies.Admin);

        group.MapGet("/", GetAll).WithName("AdminGetUsers");
        group.MapPost("/", Create).WithName("AdminCreateUser").WithValidation<AdminUserCreateDto>();
        group.MapGet("/{id:guid}", GetById).WithName("AdminGetUserById");
        group.MapPut("/{id:guid}", Update).WithName("AdminUpdateUser").WithValidation<AdminUserUpdateDto>();
        group.MapPatch("/{id:guid}/toggle-status", ToggleStatus).WithName("AdminToggleUserStatus");
        group.MapPatch("/{id:guid}/airline", SetAirline).WithName("AdminSetUserAirline").WithValidation<SetUserAirlineDto>();
        group.MapGet("/{id:guid}/roles", GetUserRoles).WithName("AdminGetUserRoles");
        group.MapPut("/{id:guid}/roles", AssignRoles).WithName("AdminAssignUserRoles").WithValidation<AssignRolesDto>();
        rolesGroup.MapGet("/", GetRealmRoles).WithName("AdminGetRealmRoles");

        return app;
    }

    private static async Task<IResult> GetAll(
        IUserService users,
        IKeycloakService keycloak,
        string? search,
        string? status,
        string? role,
        int page = 1,
        int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        bool? isActive = status switch
        {
            "active" => true,
            "inactive" => false,
            _ => null,
        };

        if (!string.IsNullOrWhiteSpace(role))
            return await GetAllByRole(users, keycloak, search, isActive, role, page, pageSize, cancellationToken);

        var (items, total) = await users.GetAllForAdminAsync(search, isActive, page, pageSize, cancellationToken);

        var rolesTasks = items.Select(u => keycloak.GetUserRolesAsync(u.KeycloakUserId, cancellationToken));
        var allRoles = await Task.WhenAll(rolesTasks);

        var dtos = items.Select((u, i) => new AdminUserListItemDto(
            u.Id,
            u.FullName,
            u.Email,
            u.PhoneNumber,
            u.IsActive,
            u.CreatedAt,
            allRoles[i])).ToList();

        return TypedResults.Ok(new PagedResult<AdminUserListItemDto>(dtos, total, page, pageSize));
    }

    private static async Task<IResult> GetAllByRole(
        IUserService users,
        IKeycloakService keycloak,
        string? search,
        bool? isActive,
        string role,
        int page,
        int pageSize,
        CancellationToken cancellationToken)
    {
        var candidates = await users.GetAllForAdminAsync(search, isActive, cancellationToken);
        var rolesTasks = candidates.Select(u => keycloak.GetUserRolesAsync(u.KeycloakUserId, cancellationToken));
        var allRoles = await Task.WhenAll(rolesTasks);

        var filtered = candidates
            .Select((user, index) => new { User = user, Roles = allRoles[index] })
            .Where(x => x.Roles.Any(r => string.Equals(r, role.Trim(), StringComparison.OrdinalIgnoreCase)))
            .ToList();

        var total = filtered.Count;
        var dtos = filtered
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(x => new AdminUserListItemDto(
                x.User.Id,
                x.User.FullName,
                x.User.Email,
                x.User.PhoneNumber,
                x.User.IsActive,
                x.User.CreatedAt,
                x.Roles))
            .ToList();

        return TypedResults.Ok(new PagedResult<AdminUserListItemDto>(dtos, total, page, pageSize));
    }

    private static async Task<IResult> Create(
        AdminUserCreateDto dto,
        IKeycloakService keycloak,
        IUserService users,
        CancellationToken cancellationToken)
    {
        var keycloakUserId = await keycloak.CreateUserAsync(dto.Email, dto.FullName, dto.Password, cancellationToken);
        var user = await users.CreateAsync(
            keycloakUserId, dto.FullName, dto.Email,
            dto.PhoneNumber, dto.DateOfBirth, dto.PassportNumber, dto.Nationality,
            cancellationToken);

        var roles = await keycloak.GetUserRolesAsync(keycloakUserId, cancellationToken);

        return TypedResults.Created(
            $"/api/v1/admin/users/{user.Id}",
            new AdminUserListItemDto(
                user.Id, user.FullName, user.Email, user.PhoneNumber,
                user.IsActive, user.CreatedAt, roles));
    }

    private static async Task<IResult> GetById(
        Guid id,
        IUserService users,
        IKeycloakService keycloak,
        CancellationToken cancellationToken)
    {
        var user = await users.GetByIdAsync(id, cancellationToken);
        if (user is null) return TypedResults.NotFound();

        var roles = await keycloak.GetUserRolesAsync(user.KeycloakUserId, cancellationToken);

        return TypedResults.Ok(new AdminUserDetailDto(
            user.Id,
            user.Email,
            user.FullName,
            user.PhoneNumber,
            user.DateOfBirth,
            user.PassportNumber,
            user.Nationality,
            user.IsActive,
            user.CreatedAt,
            user.UpdatedAt,
            roles,
            user.AirlineId));
    }

    private static async Task<IResult> Update(
        Guid id,
        AdminUserUpdateDto dto,
        IUserService users,
        CancellationToken cancellationToken)
    {
        var updated = await users.UpdateAsync(
            id, dto.FullName, dto.PhoneNumber, dto.DateOfBirth,
            dto.PassportNumber, dto.Nationality, cancellationToken);

        return updated is null ? TypedResults.NotFound() : TypedResults.Ok(new AdminUserDetailDto(
            updated.Id,
            updated.Email,
            updated.FullName,
            updated.PhoneNumber,
            updated.DateOfBirth,
            updated.PassportNumber,
            updated.Nationality,
            updated.IsActive,
            updated.CreatedAt,
            updated.UpdatedAt,
            []));
    }

    private static async Task<IResult> ToggleStatus(
        Guid id,
        IUserService users,
        IKeycloakService keycloak,
        CancellationToken cancellationToken)
    {
        var user = await users.GetByIdAsync(id, cancellationToken);
        if (user is null) return TypedResults.NotFound();

        var newStatus = !user.IsActive;
        var updated = await users.SetActiveAsync(id, newStatus, cancellationToken);
        if (updated is null) return TypedResults.NotFound();

        await keycloak.SetUserEnabledAsync(user.KeycloakUserId, newStatus, cancellationToken);
        return TypedResults.Ok(new { isActive = newStatus });
    }

    private static async Task<IResult> GetUserRoles(
        Guid id,
        IUserService users,
        IKeycloakService keycloak,
        CancellationToken cancellationToken)
    {
        var user = await users.GetByIdAsync(id, cancellationToken);
        if (user is null) return TypedResults.NotFound();

        var roles = await keycloak.GetUserRolesAsync(user.KeycloakUserId, cancellationToken);
        return TypedResults.Ok(roles);
    }

    private static async Task<IResult> AssignRoles(
        Guid id,
        AssignRolesDto dto,
        IUserService users,
        IKeycloakService keycloak,
        CancellationToken cancellationToken)
    {
        var user = await users.GetByIdAsync(id, cancellationToken);
        if (user is null) return TypedResults.NotFound();

        await keycloak.AssignUserRolesAsync(user.KeycloakUserId, dto.Roles, cancellationToken);
        var roles = await keycloak.GetUserRolesAsync(user.KeycloakUserId, cancellationToken);
        return TypedResults.Ok(roles);
    }

    private static async Task<IResult> SetAirline(
        Guid id,
        SetUserAirlineDto dto,
        IUserService users,
        CancellationToken cancellationToken)
    {
        var updated = await users.SetAirlineAsync(id, dto.AirlineId, cancellationToken);
        return updated is null ? TypedResults.NotFound() : TypedResults.Ok(new { airlineId = updated.AirlineId });
    }

    private static async Task<IResult> GetRealmRoles(
        IKeycloakService keycloak,
        CancellationToken cancellationToken)
    {
        var roles = await keycloak.GetRealmRolesAsync(cancellationToken);
        return TypedResults.Ok(roles);
    }
}
