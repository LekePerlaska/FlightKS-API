using FlightKS.Models.Dtos.Bookings;
using FlightKS.Models.Entities;

namespace FlightKS.Mappers;

public static class BookingMapping
{
    public static BookingPassenger ToEntity(this BookingPassengerCreateDto dto, decimal fare) => new()
    {
        FirstName = dto.FirstName,
        LastName = dto.LastName,
        DateOfBirth = dto.DateOfBirth,
        Nationality = dto.Nationality,
        PassportNumber = dto.PassportNumber,
        PassportExpiry = dto.PassportExpiry,
        Email = dto.Email,
        Phone = dto.Phone,
        SeatNumber = dto.SeatNumber,
        SpecialRequests = dto.SpecialRequests,
        FrequentFlyerNumber = dto.FrequentFlyerNumber,
        Fare = fare,
        PassengerType = dto.PassengerType,
    };

    public static BookingPassengerResponseDto ToResponse(this BookingPassenger entity) => new()
    {
        Id = entity.Id,
        FirstName = entity.FirstName,
        LastName = entity.LastName,
        DateOfBirth = entity.DateOfBirth,
        Nationality = entity.Nationality,
        PassportNumber = entity.PassportNumber,
        PassportExpiry = entity.PassportExpiry,
        Email = entity.Email,
        Phone = entity.Phone,
        SeatNumber = entity.SeatNumber,
        SpecialRequests = entity.SpecialRequests,
        FrequentFlyerNumber = entity.FrequentFlyerNumber,
        Fare = entity.Fare,
        PassengerType = entity.PassengerType,
    };

    public static BookingResponseDto ToResponse(this Booking entity) => new()
    {
        Id = entity.Id,
        Reference = entity.Reference,
        UserId = entity.UserId,
        Flight = entity.Flight.ToResponse(),
        Cabin = entity.Cabin,
        TripType = entity.TripType,
        Status = entity.Status,
        ReturnDate = entity.ReturnDate,
        PassengerCount = entity.PassengerCount,
        BaseFare = entity.BaseFare,
        TaxesFees = entity.TaxesFees,
        TotalPrice = entity.TotalPrice,
        BookedAt = entity.BookedAt,
        UpdatedAt = entity.UpdatedAt,
        Passengers = [.. entity.Passengers.Select(ToResponse)],
    };
}
