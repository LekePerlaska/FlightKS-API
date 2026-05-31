using FlightKS.Models.Dtos.Airlines;
using FlightKS.Models.Dtos.Airports;

namespace FlightKS.Models.Dtos.Flights;

public record FlightSearchQuery(
    Guid OriginAirportId,
    Guid DestinationAirportId,
    DateOnly Date,
    int Passengers);

public record FlightScheduleSearchResultDto(
    Guid ScheduleId,
    Guid FlightId,
    string FlightNumber,
    AirlineDto Airline,
    AirportDto Origin,
    AirportDto Destination,
    DateTime DepartureTime,
    DateTime ArrivalTime,
    int DurationMinutes,
    decimal CurrentPrice,
    int AvailableSeats);

public record FlightAdminListItemDto(
    Guid Id,
    string FlightNumber,
    Guid AirlineId,
    string AirlineName,
    string AirlineCode,
    AirportDto Origin,
    AirportDto Destination,
    decimal BasePrice,
    int DurationMinutes,
    bool IsActive,
    DateTime CreatedAt,
    DateTime UpdatedAt);

public record FlightCreateDto(
    Guid AirlineId,
    string FlightNumber,
    Guid OriginAirportId,
    Guid DestinationAirportId,
    decimal BasePrice);

public record FlightUpdateDto(
    Guid? AirlineId,
    string? FlightNumber,
    Guid? OriginAirportId,
    Guid? DestinationAirportId,
    decimal? BasePrice,
    bool? IsActive);
