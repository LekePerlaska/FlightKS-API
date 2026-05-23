using FlightKS.Models.Dtos.Airports;
using FlightKS.Models.Entities;

namespace FlightKS.Mappers;

public static class AirportMapping
{
    public static AirportDto ToDto(this Airport a) =>
        new(a.Id, a.Code, a.Name, a.City, a.Country, a.TimeZone);

    public static AirportAdminListItemDto ToAdminListItem(this Airport a) =>
        new(a.Id, a.Code, a.Name, a.City, a.Country, a.TimeZone, a.IsActive, a.CreatedAt, a.UpdatedAt);
}
