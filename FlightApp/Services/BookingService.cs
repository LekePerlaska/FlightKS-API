using FlightKS.Data;
using FlightKS.Enums;
using FlightKS.Mappers;
using FlightKS.Models.Dtos.Bookings;
using FlightKS.Models.Entities;
using FlightKS.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace FlightKS.Services;

public class BookingService(AppDbContext db) : IBookingService
{
    public async Task<BookingResponseDto> CreateAsync(Guid userId, BookingCreateDto dto, CancellationToken cancellationToken = default)
    {
        var flight = await db.Flights
            .Include(f => f.OriginAirport)
            .Include(f => f.DestinationAirport)
            .Include(f => f.Prices)
            .FirstOrDefaultAsync(f => f.Id == dto.FlightId, cancellationToken)
            ?? throw new InvalidOperationException($"Flight '{dto.FlightId}' not found.");

        var price = flight.Prices.FirstOrDefault(p => p.Cabin == dto.CabinClass)
            ?? throw new InvalidOperationException($"Flight '{dto.FlightId}' has no '{dto.CabinClass}' cabin.");

        var perPassengerFare = dto.Passengers.Count == 0 ? 0m : dto.PaymentSummary.BaseFare / dto.Passengers.Count;

        var booking = new Booking
        {
            UserId = userId,
            FlightId = flight.Id,
            Flight = flight,
            Reference = GenerateReference(),
            Cabin = dto.CabinClass,
            TripType = dto.TripType,
            Status = BookingStatus.Upcoming,
            ReturnDate = dto.ReturnDate,
            PassengerCount = (short)dto.Passengers.Count,
            BaseFare = dto.PaymentSummary.BaseFare,
            TaxesFees = dto.PaymentSummary.TaxesFees,
            TotalPrice = dto.PaymentSummary.TotalPrice,
            Passengers = [.. dto.Passengers.Select(p => p.ToEntity(perPassengerFare))],
        };

        db.Bookings.Add(booking);
        await db.SaveChangesAsync(cancellationToken);

        return booking.ToResponse();
    }

    public async Task<IEnumerable<BookingResponseDto>> GetAllForUserAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var bookings = await LoadBookings(asNoTracking: true)
            .Where(b => b.UserId == userId)
            .OrderByDescending(b => b.BookedAt)
            .ToListAsync(cancellationToken);

        return bookings.Select(BookingMapping.ToResponse);
    }

    public async Task<BookingResponseDto?> GetByIdAsync(Guid userId, Guid id, CancellationToken cancellationToken = default)
    {
        var booking = await LoadBookings(asNoTracking: true)
            .FirstOrDefaultAsync(b => b.UserId == userId && b.Id == id, cancellationToken);

        return booking?.ToResponse();
    }

    private IQueryable<Booking> LoadBookings(bool asNoTracking)
    {
        var q = db.Bookings
            .Include(b => b.Passengers)
            .Include(b => b.Flight).ThenInclude(f => f.OriginAirport)
            .Include(b => b.Flight).ThenInclude(f => f.DestinationAirport)
            .Include(b => b.Flight).ThenInclude(f => f.Prices)
            .AsQueryable();

        return asNoTracking ? q.AsNoTracking() : q;
    }

    private static string GenerateReference()
    {
        const string alphabet = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789";
        Span<char> chars = stackalloc char[4];
        var rng = Random.Shared;
        for (var i = 0; i < chars.Length; i++)
            chars[i] = alphabet[rng.Next(alphabet.Length)];
        return $"BKG-{new string(chars)}";
    }
}
