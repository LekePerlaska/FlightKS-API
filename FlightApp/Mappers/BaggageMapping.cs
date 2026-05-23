using FlightKS.Models.Dtos.BaggageOptions;
using FlightKS.Models.Entities;

namespace FlightKS.Mappers;

public static class BaggageMapping
{
    public static BaggageOptionDto ToDto(this BaggageOption b) =>
        new(b.Id, b.Name, b.WeightKg, b.Price, b.Description);
}
