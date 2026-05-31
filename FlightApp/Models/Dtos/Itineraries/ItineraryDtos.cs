using FlightKS.Enums;
using FlightKS.Models.Dtos.Airlines;
using FlightKS.Models.Dtos.Airports;

namespace FlightKS.Models.Dtos.Itineraries;

public record ItinerarySegmentDto(
    Guid Id,
    int SegmentOrder,
    int? LayoverMinutesAfterSegment,
    Guid FlightScheduleId,
    Guid FlightId,
    string FlightNumber,
    AirlineDto Airline,
    AirportDto OriginAirport,
    AirportDto DestinationAirport,
    DateTime DepartureTime,
    DateTime ArrivalTime,
    int DurationMinutes,
    FlightScheduleStatus Status,
    int AvailableSeats,
    decimal CurrentPrice,
    string? Gate,
    string? DelayReason);

public record ItinerarySearchResultDto(
    Guid Id,
    Guid OriginAirportId,
    Guid DestinationAirportId,
    AirportDto OriginAirport,
    AirportDto DestinationAirport,
    DateTime DepartureTime,
    DateTime ArrivalTime,
    int TotalDurationMinutes,
    decimal TotalPrice,
    int StopsCount,
    bool IsActive,
    DateTime CreatedAt,
    DateTime UpdatedAt,
    IReadOnlyList<ItinerarySegmentDto> Segments);

// An itinerary is defined entirely by the ordered flight schedules it contains.
// Route, times, duration, price and stops are all derived from those segments —
// none of them are entered by hand.
public record ItineraryCreateDto(
    List<Guid> FlightScheduleIds);

// The only itinerary-level field an admin can set directly is visibility.
// Everything else changes by editing the itinerary's segments.
public record ItineraryUpdateDto(
    bool? IsActive);

public record ItinerarySegmentCreateDto(
    Guid FlightScheduleId,
    int SegmentOrder,
    int? LayoverMinutesAfterSegment);

public record ItinerarySegmentUpdateDto(
    Guid? FlightScheduleId,
    int? SegmentOrder,
    int? LayoverMinutesAfterSegment);

public record ItinerarySeatSummarySegmentDto(
    Guid SegmentId,
    int SegmentOrder,
    AirportDto OriginAirport,
    AirportDto DestinationAirport,
    int TotalSeats,
    int AvailableSeats,
    Dictionary<SeatClass, int> AvailableByClass);

public record ItinerarySeatSummaryDto(
    IReadOnlyList<ItinerarySeatSummarySegmentDto> Segments);
