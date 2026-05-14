using FlightKS.Models.Dtos.Flights;
using FlightKS.Models.Entities;

namespace FlightKS.Mappers;

public static class FlightMapping
{
    public static Flight ToEntity(this FlightCreateDto dto) => new()
    {
        FlightNumber = dto.FlightNumber,
        AirlineName = dto.AirlineName,
        AirlineCode = dto.AirlineCode,
        AirlineLogoUrl = dto.AirlineLogoUrl,
        DepartureAirportCode = dto.DepartureAirportCode,
        DepartureCity = dto.DepartureCity,
        DepartureTime = dto.DepartureTime,
        ArrivalAirportCode = dto.ArrivalAirportCode,
        ArrivalCity = dto.ArrivalCity,
        ArrivalTime = dto.ArrivalTime,
        CabinClass = dto.CabinClass,
        Price = dto.Price,
        Currency = dto.Currency,
        CabinBaggageKg = dto.CabinBaggageKg,
        CheckedBaggageKg = dto.CheckedBaggageKg,
        Status = dto.Status,
    };

    public static FlightResponseDto ToResponse(this Flight entity) => new()
    {
        Id = entity.Id,
        FlightNumber = entity.FlightNumber,
        AirlineName = entity.AirlineName,
        AirlineCode = entity.AirlineCode,
        AirlineLogoUrl = entity.AirlineLogoUrl,
        DepartureAirportCode = entity.DepartureAirportCode,
        DepartureCity = entity.DepartureCity,
        DepartureTime = entity.DepartureTime,
        ArrivalAirportCode = entity.ArrivalAirportCode,
        ArrivalCity = entity.ArrivalCity,
        ArrivalTime = entity.ArrivalTime,
        CabinClass = entity.CabinClass,
        Price = entity.Price,
        Currency = entity.Currency,
        CabinBaggageKg = entity.CabinBaggageKg,
        CheckedBaggageKg = entity.CheckedBaggageKg,
        Status = entity.Status,
    };

    public static void ApplyTo(this FlightUpdateDto dto, Flight entity)
    {
        entity.FlightNumber = dto.FlightNumber;
        entity.AirlineName = dto.AirlineName;
        entity.AirlineCode = dto.AirlineCode;
        entity.AirlineLogoUrl = dto.AirlineLogoUrl;
        entity.DepartureAirportCode = dto.DepartureAirportCode;
        entity.DepartureCity = dto.DepartureCity;
        entity.DepartureTime = dto.DepartureTime;
        entity.ArrivalAirportCode = dto.ArrivalAirportCode;
        entity.ArrivalCity = dto.ArrivalCity;
        entity.ArrivalTime = dto.ArrivalTime;
        entity.CabinClass = dto.CabinClass;
        entity.Price = dto.Price;
        entity.Currency = dto.Currency;
        entity.CabinBaggageKg = dto.CabinBaggageKg;
        entity.CheckedBaggageKg = dto.CheckedBaggageKg;
        entity.Status = dto.Status;
    }
}
