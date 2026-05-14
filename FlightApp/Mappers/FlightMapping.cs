using FlightKS.Models.Dtos.Flights;
using FlightKS.Models.Entities;

namespace FlightKS.Mappers;

public static class FlightMapping
{
    public static FlightResponseDto ToResponse(this Flight entity) => new()
    {
        Id = entity.Id,
        Origin = entity.OriginAirport.ToDto(),
        Destination = entity.DestinationAirport.ToDto(),
        Airline = entity.Airline,
        FlightNumber = entity.FlightNumber,
        Currency = entity.Currency,
        DepartureTime = entity.DepartureTime,
        ArrivalTime = entity.ArrivalTime,
        DurationMinutes = entity.DurationMinutes,
        Stops = entity.Stops,
        BaggageIncluded = entity.BaggageIncluded,
        Refundable = entity.Refundable,
        Prices = [.. entity.Prices.Select(p => new FlightPriceDto
        {
            Cabin = p.Cabin,
            Price = p.Price,
            TotalSeats = p.TotalSeats,
        })],
    };
}
