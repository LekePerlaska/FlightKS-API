using FlightKS.Models.Dtos.Itineraries;
using FlightKS.Models.Entities;

namespace FlightKS.Mappers;

public static class ItineraryMapping
{
    private static int DurationMinutes(DateTime departure, DateTime arrival) =>
        Math.Max(0, (int)Math.Round((arrival - departure).TotalMinutes));

    public static ItinerarySearchResultDto ToSearchResult(this Itinerary i) => new(
        i.Id,
        i.OriginAirportId,
        i.DestinationAirportId,
        i.OriginAirport.ToDto(),
        i.DestinationAirport.ToDto(),
        i.DepartureTime,
        i.ArrivalTime,
        i.TotalDurationMinutes,
        i.TotalPrice,
        i.StopsCount,
        i.IsActive,
        i.CreatedAt,
        i.UpdatedAt,
        i.Segments
            .OrderBy(s => s.SegmentOrder)
            .Select(s => s.ToDto())
            .ToArray());

    public static ItinerarySegmentDto ToDto(this ItinerarySegment s) => new(
        s.Id,
        s.SegmentOrder,
        s.LayoverMinutesAfterSegment,
        s.FlightScheduleId,
        s.FlightSchedule.FlightId,
        s.FlightSchedule.Flight.FlightNumber,
        s.FlightSchedule.Flight.Airline.ToDto(),
        s.FlightSchedule.Flight.OriginAirport.ToDto(),
        s.FlightSchedule.Flight.DestinationAirport.ToDto(),
        s.FlightSchedule.DepartureTime,
        s.FlightSchedule.ArrivalTime,
        DurationMinutes(s.FlightSchedule.DepartureTime, s.FlightSchedule.ArrivalTime),
        s.FlightSchedule.Status,
        s.FlightSchedule.AvailableSeats,
        s.FlightSchedule.CurrentPrice,
        s.FlightSchedule.Gate,
        s.FlightSchedule.DelayReason);
}
