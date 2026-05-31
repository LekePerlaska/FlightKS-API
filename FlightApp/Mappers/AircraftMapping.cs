using FlightKS.Models.Dtos.Aircrafts;
using FlightKS.Models.Entities;

namespace FlightKS.Mappers;

public static class AircraftMapping
{
    public static AircraftDto ToDto(this Aircraft a) => new(
        a.Id,
        a.AirlineId,
        a.Airline?.Name ?? string.Empty,
        a.Model,
        a.RegistrationNumber,
        a.TotalSeats,
        a.IsActive,
        a.CreatedAt,
        a.UpdatedAt);

    public static SeatAdminDto ToAdminDto(this Seat s) => new(
        s.Id,
        s.SeatNumber,
        s.SeatClass,
        s.IsWindow,
        s.IsAisle,
        s.ExtraLegroom);
}
