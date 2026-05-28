using FlightKS.Models.Dtos.Airlines;
using FlightKS.Models.Entities;

namespace FlightKS.Mappers;

public static class AirlineMapping
{
    public static AirlineDto ToDto(this Airline a) =>
        new(a.Id, a.Code, a.Name, a.Country, a.LogoFileId, a.LogoFile?.StoragePath);

    public static AirlineAdminListItemDto ToAdminListItem(this Airline a) =>
        new(a.Id, a.Code, a.Name, a.Country, a.LogoFileId, a.LogoFile?.StoragePath, a.IsActive, a.CreatedAt, a.UpdatedAt);
}
