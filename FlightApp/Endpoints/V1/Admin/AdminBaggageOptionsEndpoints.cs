using FlightKS.Auth;
using FlightKS.Endpoints;
using FlightKS.Exceptions;
using FlightKS.Mappers;
using FlightKS.Models.Dtos.BaggageOptions;
using FlightKS.Services.Interfaces;

namespace FlightKS.Endpoints.V1.Admin;

public static class AdminBaggageOptionsEndpoints
{
    public static IEndpointRouteBuilder MapAdminBaggageOptionsEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/admin/baggage-options").WithTags("AdminBaggageOptions").RequireAuthorization(Policies.Admin);

        group.MapGet("/", GetAll).WithName("AdminGetBaggageOptions");
        group.MapPost("/", Create).WithName("AdminCreateBaggageOption").WithValidation<BaggageOptionCreateDto>();
        group.MapPut("/{id:guid}", Update).WithName("AdminUpdateBaggageOption").WithValidation<BaggageOptionUpdateDto>();
        group.MapPatch("/{id:guid}", ToggleStatus).WithName("AdminToggleBaggageOptionStatus").WithValidation<BaggageOptionUpdateDto>();
        group.MapDelete("/{id:guid}", Delete).WithName("AdminDeleteBaggageOption");
        group.MapPatch("/{id:guid}/restore", Restore).WithName("AdminRestoreBaggageOption");

        return app;
    }

    private static async Task<IResult> GetAll(IBaggageOptionService baggage, CancellationToken cancellationToken)
    {
        var list = await baggage.GetAllForAdminAsync(cancellationToken);
        return TypedResults.Ok(list.Select(b => b.ToAdminListItem()));
    }

    private static async Task<IResult> Create(BaggageOptionCreateDto dto, IBaggageOptionService baggage, CancellationToken cancellationToken)
    {
        var option = await baggage.CreateAsync(dto.Name, dto.WeightKg, dto.Price, dto.Description, cancellationToken);
        return TypedResults.Created($"/api/v1/admin/baggage-options/{option.Id}", option.ToAdminListItem());
    }

    private static async Task<IResult> Update(Guid id, BaggageOptionUpdateDto dto, IBaggageOptionService baggage, CancellationToken cancellationToken)
    {
        var updated = await baggage.UpdateAsync(id, dto.Name, dto.WeightKg, dto.Price, dto.Description, dto.IsActive, cancellationToken);
        return updated is null ? TypedResults.NotFound() : TypedResults.Ok(updated.ToAdminListItem());
    }

    private static async Task<IResult> ToggleStatus(Guid id, BaggageOptionUpdateDto dto, IBaggageOptionService baggage, CancellationToken cancellationToken)
    {
        var updated = await baggage.UpdateAsync(id, null, null, null, null, dto.IsActive, cancellationToken);
        return updated is null ? TypedResults.NotFound() : TypedResults.Ok(updated.ToAdminListItem());
    }

    private static async Task<IResult> Delete(Guid id, IBaggageOptionService baggage, CancellationToken cancellationToken)
    {
        var existing = await baggage.GetByIdForAdminAsync(id, cancellationToken);
        if (existing is null) return TypedResults.NotFound();
        if (!existing.IsActive && existing.DeletedAt.HasValue)
            throw new BusinessRuleException("Baggage option is already deactivated.");

        var deleted = await baggage.DeleteAsync(id, cancellationToken);
        return deleted
            ? TypedResults.Ok(new { message = "Baggage option deactivated successfully." })
            : TypedResults.NotFound();
    }

    private static async Task<IResult> Restore(Guid id, IBaggageOptionService baggage, CancellationToken cancellationToken)
    {
        var existing = await baggage.GetByIdForAdminAsync(id, cancellationToken);
        if (existing is null) return TypedResults.NotFound();
        if (existing.IsActive && existing.DeletedAt is null)
            throw new BusinessRuleException("Baggage option is already active.");

        var restored = await baggage.RestoreAsync(id, cancellationToken);
        return restored is null ? TypedResults.NotFound() : TypedResults.Ok(restored.ToAdminListItem());
    }
}
