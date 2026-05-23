using FlightKS.Models.Dtos.Flights;
using FlightKS.Models.Entities;

namespace FlightKS.Mappers;

public static class FlightMapping
{
    public static FlightScheduleSearchResultDto ToSearchResult(this FlightSchedule s) => new(
        s.Id,
        s.FlightId,
        s.Flight.FlightNumber,
        s.Flight.Airline.ToDto(),
        s.Flight.OriginAirport.ToDto(),
        s.Flight.DestinationAirport.ToDto(),
        s.DepartureTime,
        s.ArrivalTime,
        s.Flight.DurationMinutes,
        s.CurrentPrice,
        s.AvailableSeats);

    public static FlightAdminListItemDto ToAdminListItem(this Flight f) => new(
        f.Id,
        f.FlightNumber,
        f.AirlineId,
        f.Airline?.Name ?? string.Empty,
        f.OriginAirport.ToDto(),
        f.DestinationAirport.ToDto(),
        f.BasePrice,
        f.DurationMinutes,
        f.IsActive,
        f.CreatedAt,
        f.UpdatedAt);
}
