using FlightKS.Models.Dtos.Bookings;

namespace FlightKS.Services.Interfaces;

public interface IBookingService
{
    Task<BookingResponseDto> CreateAsync(Guid userId, BookingCreateDto dto, CancellationToken cancellationToken = default);
    Task<IEnumerable<BookingResponseDto>> GetAllForUserAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<BookingResponseDto?> GetByIdAsync(Guid userId, Guid id, CancellationToken cancellationToken = default);
}
