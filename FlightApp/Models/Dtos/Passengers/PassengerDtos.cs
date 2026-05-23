namespace FlightKS.Models.Dtos.Passengers;

public record PassengerCreateDto(
    string FirstName,
    string LastName,
    DateOnly DateOfBirth,
    string? Gender,
    string? PassportNumber,
    string? Nationality);

public record PassengerUpdateDto(
    string? FirstName,
    string? LastName,
    DateOnly? DateOfBirth,
    string? Gender,
    string? PassportNumber,
    string? Nationality);

public record PassengerResponseDto(
    Guid Id,
    Guid BookingId,
    string FirstName,
    string LastName,
    DateOnly DateOfBirth,
    string? Gender,
    string? PassportNumber,
    string? Nationality,
    DateTime CreatedAt);
