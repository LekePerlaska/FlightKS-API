using FlightKS.Enums;
using FlightKS.Models.Dtos.FlightManager;
using FlightKS.Models.Dtos.FlightSchedules;
using FlightKS.Models.Entities;

namespace FlightKS.Mappers;

public static class FlightScheduleMapping
{
    private static int DurationMinutes(DateTime departure, DateTime arrival) =>
        Math.Max(0, (int)Math.Round((arrival - departure).TotalMinutes));

    public static FlightScheduleDetailDto ToDetail(this FlightSchedule s) => new(
        s.Id,
        s.FlightId,
        s.Flight.FlightNumber,
        s.Flight.Airline.ToDto(),
        s.Flight.OriginAirport.ToDto(),
        s.Flight.DestinationAirport.ToDto(),
        s.Aircraft.ToDto(),
        s.DepartureTime,
        s.ArrivalTime,
        DurationMinutes(s.DepartureTime, s.ArrivalTime),
        s.Status,
        s.AvailableSeats,
        s.CurrentPrice,
        s.Gate,
        s.DelayReason);

    public static FlightScheduleAdminListItemDto ToAdminListItem(this FlightSchedule s) => new(
        s.Id,
        s.FlightId,
        s.Flight.FlightNumber,
        s.Flight.Airline?.Name ?? string.Empty,
        s.Flight.Airline?.Code ?? string.Empty,
        s.Flight.OriginAirport?.Code ?? string.Empty,
        s.Flight.OriginAirport?.City ?? string.Empty,
        s.Flight.DestinationAirport?.Code ?? string.Empty,
        s.Flight.DestinationAirport?.City ?? string.Empty,
        s.AircraftId,
        s.Aircraft?.Model,
        s.DepartureTime,
        s.ArrivalTime,
        DurationMinutes(s.DepartureTime, s.ArrivalTime),
        s.Status,
        s.AvailableSeats,
        s.CurrentPrice,
        s.Gate,
        s.DelayReason,
        s.CreatedAt,
        s.UpdatedAt);

    public static FlightManagerScheduleListItemDto ToManagerListItem(this FlightSchedule s) => new(
        s.Id,
        s.FlightId,
        s.Flight.FlightNumber,
        s.Flight.OriginAirport.Code,
        s.Flight.DestinationAirport.Code,
        s.DepartureTime,
        s.ArrivalTime,
        s.Status,
        s.AvailableSeats,
        s.Gate);

    public static FlightSeatDto ToDto(this FlightSeat fs) => new(
        fs.Id,
        fs.SeatId,
        fs.Seat.SeatNumber,
        fs.Seat.SeatClass,
        fs.Seat.IsWindow,
        fs.Seat.IsAisle,
        fs.Seat.ExtraLegroom,
        fs.Status,
        fs.Price,
        fs.ReservedUntil);

    public static ScheduleSeatDto ToScheduleSeatDto(this Seat s, decimal price) => new(
        s.Id,
        s.SeatNumber,
        s.SeatClass,
        s.IsWindow,
        s.IsAisle,
        s.ExtraLegroom,
        price);
}
