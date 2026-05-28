namespace FlightKS.Models.Dtos.Timezones;

public record TimezoneDto(
    string Id,
    string Label,
    string Region,
    string City,
    string CurrentOffset);
