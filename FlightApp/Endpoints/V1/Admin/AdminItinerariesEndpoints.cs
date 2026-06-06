using FlightKS.Auth;
using FlightKS.Endpoints;
using FlightKS.Mappers;
using FlightKS.Models.Dtos.Itineraries;
using FlightKS.Services.Interfaces;

namespace FlightKS.Endpoints.V1.Admin;

public static class AdminItinerariesEndpoints
{
    public static IEndpointRouteBuilder MapAdminItinerariesEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/admin/itineraries")
            .WithTags("AdminItineraries")
            .RequireAuthorization(Policies.Admin);

        group.MapGet("/", GetAll).WithName("AdminGetItineraries");
        group.MapGet("/{id:guid}", GetById).WithName("AdminGetItineraryById");
        group.MapPost("/", Create).WithName("AdminCreateItinerary").WithValidation<ItineraryCreateDto>();
        group.MapPatch("/{id:guid}", ToggleStatus).WithName("AdminToggleItineraryStatus").WithValidation<ItineraryUpdateDto>();
        group.MapDelete("/{id:guid}", Delete).WithName("AdminDeleteItinerary");
        group.MapGet("/{id:guid}/segments", GetSegments).WithName("AdminGetItinerarySegments");
        group.MapPost("/{id:guid}/segments", AddSegment).WithName("AdminAddItinerarySegment").WithValidation<ItinerarySegmentCreateDto>();

        var segGroup = app.MapGroup("/admin/itinerary-segments")
            .WithTags("AdminItineraries")
            .RequireAuthorization(Policies.Admin);

        segGroup.MapPut("/{segmentId:guid}", UpdateSegment).WithName("AdminUpdateItinerarySegment").WithValidation<ItinerarySegmentUpdateDto>();
        segGroup.MapDelete("/{segmentId:guid}", DeleteSegment).WithName("AdminDeleteItinerarySegment");

        return app;
    }

    private static async Task<IResult> GetAll(IItineraryService itineraries, CancellationToken cancellationToken)
    {
        var list = await itineraries.GetAllForAdminAsync(cancellationToken);
        return TypedResults.Ok(list.Select(i => i.ToSearchResult()));
    }

    private static async Task<IResult> GetById(Guid id, IItineraryService itineraries, CancellationToken cancellationToken)
    {
        var itinerary = await itineraries.GetByIdAsync(id, cancellationToken);
        return itinerary is null ? TypedResults.NotFound() : TypedResults.Ok(itinerary.ToSearchResult());
    }

    private static async Task<IResult> Create(ItineraryCreateDto dto, IItineraryService itineraries, CancellationToken cancellationToken)
    {
        var itinerary = await itineraries.CreateFromSchedulesAsync(dto.FlightScheduleIds, cancellationToken);
        return TypedResults.Created($"/api/v1/admin/itineraries/{itinerary.Id}", itinerary.ToSearchResult());
    }

    private static async Task<IResult> ToggleStatus(Guid id, ItineraryUpdateDto dto, IItineraryService itineraries, CancellationToken cancellationToken)
    {
        var updated = await itineraries.SetActiveAsync(id, dto.IsActive!.Value, cancellationToken);
        return updated is null ? TypedResults.NotFound() : TypedResults.Ok(updated.ToSearchResult());
    }

    private static async Task<IResult> Delete(Guid id, IItineraryService itineraries, CancellationToken cancellationToken)
    {
        var deleted = await itineraries.DeleteForAdminAsync(id, cancellationToken);
        return deleted ? TypedResults.NoContent() : TypedResults.NotFound();
    }

    private static async Task<IResult> GetSegments(Guid id, IItineraryService itineraries, CancellationToken cancellationToken)
    {
        var segments = await itineraries.GetSegmentsAsync(id, cancellationToken);
        return TypedResults.Ok(segments.Select(s => s.ToDto()));
    }

    private static async Task<IResult> AddSegment(Guid id, ItinerarySegmentCreateDto dto, IItineraryService itineraries, CancellationToken cancellationToken)
    {
        var segment = await itineraries.AddSegmentAsync(
            id, dto.FlightScheduleId, dto.SegmentOrder, dto.LayoverMinutesAfterSegment, cancellationToken);
        return TypedResults.Created($"/api/v1/admin/itinerary-segments/{segment.Id}", segment.ToDto());
    }

    private static async Task<IResult> UpdateSegment(Guid segmentId, ItinerarySegmentUpdateDto dto, IItineraryService itineraries, CancellationToken cancellationToken)
    {
        var updated = await itineraries.UpdateSegmentAsync(
            segmentId, dto.FlightScheduleId, dto.SegmentOrder, dto.LayoverMinutesAfterSegment, cancellationToken);
        return updated is null ? TypedResults.NotFound() : TypedResults.Ok(updated.ToDto());
    }

    private static async Task<IResult> DeleteSegment(Guid segmentId, IItineraryService itineraries, CancellationToken cancellationToken)
    {
        var deleted = await itineraries.DeleteSegmentAsync(segmentId, cancellationToken);
        return deleted ? TypedResults.NoContent() : TypedResults.NotFound();
    }
}
