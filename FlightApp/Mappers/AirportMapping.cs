using FlightKS.Models.Dtos.Flights;
using FlightKS.Models.Entities;

namespace FlightKS.Mappers;

public static class AirportMapping
{
    public static AirportDto ToDto(this Airport entity) => new()
    {
        Code = entity.Code,
        City = entity.City,
        Country = entity.Country,
        Name = entity.Name,
        Timezone = entity.Timezone,
    };
}
