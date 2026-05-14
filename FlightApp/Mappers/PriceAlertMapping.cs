using FlightKS.Models.Dtos.Alerts;
using FlightKS.Models.Entities;

namespace FlightKS.Mappers;

public static class PriceAlertMapping
{
    public static PriceAlert ToEntity(this PriceAlertCreateDto dto, Guid userId) => new()
    {
        UserId = userId,
        Origin = dto.Origin,
        Destination = dto.Destination,
        Cabin = dto.Cabin,
        TargetPrice = dto.TargetPrice,
        DateFrom = dto.DateFrom,
        DateTo = dto.DateTo,
    };

    public static PriceAlertResponseDto ToResponse(this PriceAlert entity) => new()
    {
        Id = entity.Id,
        UserId = entity.UserId,
        Origin = entity.OriginAirport.ToDto(),
        Destination = entity.DestinationAirport.ToDto(),
        Cabin = entity.Cabin,
        TargetPrice = entity.TargetPrice,
        CurrentPrice = entity.CurrentPrice,
        DateFrom = entity.DateFrom,
        DateTo = entity.DateTo,
        Active = entity.Active,
        CreatedAt = entity.CreatedAt,
    };
}
