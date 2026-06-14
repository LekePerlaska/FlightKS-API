using FlightKS.Auth;
using FlightKS.Data;
using FlightKS.Endpoints;
using FlightKS.Enums;
using FlightKS.Mappers;
using FlightKS.Models.Dtos;
using FlightKS.Models.Dtos.Bookings;
using FlightKS.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace FlightKS.Endpoints.V1.Admin;

public static class AdminBookingsEndpoints
{
    public static IEndpointRouteBuilder MapAdminBookingsEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/admin/bookings").WithTags("AdminBookings").RequireAuthorization(Policies.Admin);

        group.MapGet("/", GetAll).WithName("AdminGetBookings");
        group.MapGet("/{id:guid}", GetById).WithName("AdminGetBookingById");
        group.MapPatch("/{id:guid}", UpdateStatus).WithName("AdminUpdateBookingStatus").WithValidation<BookingStatusUpdateDto>();
        group.MapGet("/{id:guid}/audit-logs", GetAuditLogs).WithName("AdminGetBookingAuditLogs");

        return app;
    }

    private static async Task<IResult> GetAll(
        IBookingService bookings,
        CancellationToken cancellationToken,
        string? search = null,
        BookingStatus? status = null,
        PaymentStatus? paymentStatus = null,
        DateOnly? createdDate = null,
        int page = 1,
        int pageSize = 20)
    {
        var (items, total) = await bookings.GetAllForAdminAsync(
            search, status, paymentStatus, createdDate, page, pageSize, cancellationToken);
        return TypedResults.Ok(new PagedResult<AdminBookingListItemDto>(items.Select(b => b.ToAdminListItem()).ToList(), total, page, pageSize));
    }

    private static async Task<IResult> GetById(Guid id, IBookingService bookings, CancellationToken cancellationToken)
    {
        var booking = await bookings.GetDetailForAdminAsync(id, cancellationToken);
        return booking is null ? TypedResults.NotFound() : TypedResults.Ok(booking.ToConfirmation());
    }

    private static async Task<IResult> UpdateStatus(Guid id, BookingStatusUpdateDto dto, IBookingService bookings, CancellationToken cancellationToken)
    {
        var updated = await bookings.UpdateStatusAsync(id, dto.Status, cancellationToken);
        return updated is null ? TypedResults.NotFound() : TypedResults.Ok(updated.ToResponse());
    }

    private static async Task<IResult> GetAuditLogs(Guid id, AppDbContext db, CancellationToken cancellationToken)
    {
        var logs = await db.AuditLogs.AsNoTracking()
            .Where(l => l.EntityName == "Booking" && l.EntityId == id)
            .OrderByDescending(l => l.CreatedAt)
            .Select(l => new
            {
                l.Id,
                l.UserId,
                l.EntityName,
                l.EntityId,
                l.Action,
                l.OldValues,
                l.NewValues,
                l.CreatedAt,
            })
            .ToListAsync(cancellationToken);

        return TypedResults.Ok(logs);
    }
}
