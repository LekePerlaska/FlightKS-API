using FlightKS.Enums;
using FlightKS.Mappers;
using FlightKS.Models.Dtos.FlightSchedules;
using FlightKS.Models.Dtos.Itineraries;
using FlightKS.Services.Interfaces;

namespace FlightKS.Endpoints.V1;

public static class ItinerariesEndpoints
{
    public static IEndpointRouteBuilder MapItinerariesEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/itineraries").WithTags("Itineraries");

        group.MapGet("/search", Search).WithName("SearchItineraries");
        group.MapGet("/{id:guid}", GetById).WithName("GetItinerary");
        group.MapGet("/{id:guid}/segments", GetSegments).WithName("GetItinerarySegments");
        group.MapGet("/{id:guid}/seat-summary", GetSeatSummary).WithName("GetItinerarySeatSummary");
        group.MapGet("/{id:guid}/segments/{segmentId:guid}/seats", GetSegmentSeats)
            .WithName("GetItinerarySegmentSeats");

        return app;
    }

    private static async Task<IResult> Search(
        Guid originAirportId,
        Guid destinationAirportId,
        DateOnly departureDate,
        IItineraryService itineraries,
        CancellationToken cancellationToken,
        int passengers = 1)
    {
        var results = await itineraries.SearchAsync(
            originAirportId, destinationAirportId, departureDate, passengers, cancellationToken);
        return TypedResults.Ok(results.Select(i => i.ToSearchResult()));
    }

    private static async Task<IResult> GetById(
        Guid id,
        IItineraryService itineraries,
        CancellationToken cancellationToken)
    {
        var itinerary = await itineraries.GetByIdAsync(id, cancellationToken);
        return itinerary is null
            ? TypedResults.NotFound()
            : TypedResults.Ok(itinerary.ToSearchResult());
    }

    private static async Task<IResult> GetSegments(
        Guid id,
        IItineraryService itineraries,
        CancellationToken cancellationToken)
    {
        var segments = await itineraries.GetSegmentsAsync(id, cancellationToken);
        return TypedResults.Ok(segments.Select(s => s.ToDto()));
    }

    private static async Task<IResult> GetSeatSummary(
        Guid id,
        IItineraryService itineraries,
        IFlightScheduleService schedules,
        CancellationToken cancellationToken)
    {
        var segments = (await itineraries.GetSegmentsAsync(id, cancellationToken)).ToList();
        if (segments.Count == 0) return TypedResults.NotFound();

        var summaries = new List<ItinerarySeatSummarySegmentDto>();
        foreach (var seg in segments)
        {
            var summary = await schedules.GetSeatSummaryAsync(seg.FlightScheduleId, cancellationToken);
            if (summary is null) continue;
            summaries.Add(new ItinerarySeatSummarySegmentDto(
                seg.Id,
                seg.SegmentOrder,
                seg.FlightSchedule.Flight.OriginAirport.ToDto(),
                seg.FlightSchedule.Flight.DestinationAirport.ToDto(),
                summary.Total,
                summary.Available,
                new Dictionary<SeatClass, int>(summary.AvailableByClass)));
        }

        return TypedResults.Ok(new ItinerarySeatSummaryDto([.. summaries]));
    }

    private static async Task<IResult> GetSegmentSeats(
        Guid id,
        Guid segmentId,
        IItineraryService itineraries,
        IFlightScheduleService schedules,
        CancellationToken cancellationToken)
    {
        var segment = (await itineraries.GetSegmentsAsync(id, cancellationToken))
            .FirstOrDefault(s => s.Id == segmentId);

        if (segment is null) return TypedResults.NotFound();

        var schedule = await schedules.GetByIdAsync(segment.FlightScheduleId, cancellationToken);
        var price = schedule?.CurrentPrice ?? 0;
        var seats = await schedules.GetSeatsAsync(segment.FlightScheduleId, cancellationToken);
        return TypedResults.Ok(seats.Select(s => s.ToScheduleSeatDto(price)));
    }
}
