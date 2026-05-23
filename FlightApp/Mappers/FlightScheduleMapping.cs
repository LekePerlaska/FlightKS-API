using FlightKS.Enums;
using FlightKS.Models.Dtos.FlightManager;
using FlightKS.Models.Dtos.FlightSchedules;
using FlightKS.Models.Entities;

namespace FlightKS.Mappers;

public static class FlightScheduleMapping
{
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
        s.Flight.DurationMinutes,
        s.Status,
        s.AvailableSeats,
        s.CurrentPrice,
        s.Gate,
        s.DelayReason);

    public static FlightScheduleAdminListItemDto ToAdminListItem(this FlightSchedule s) => new(
        s.Id,
        s.FlightId,
        s.Flight.FlightNumber,
        s.DepartureTime,
        s.ArrivalTime,
        s.Status,
        s.AvailableSeats,
        s.CurrentPrice,
        s.Gate);

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
}
