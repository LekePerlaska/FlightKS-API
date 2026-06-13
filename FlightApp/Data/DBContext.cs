using FlightKS.Enums;
using FlightKS.Models.Entities;
using Microsoft.EntityFrameworkCore;

namespace FlightKS.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<User> Users => Set<User>();
    public DbSet<Airline> Airlines => Set<Airline>();
    public DbSet<Airport> Airports => Set<Airport>();
    public DbSet<Aircraft> Aircrafts => Set<Aircraft>();
    public DbSet<Flight> Flights => Set<Flight>();
    public DbSet<FlightSchedule> FlightSchedules => Set<FlightSchedule>();
    public DbSet<FlightSchedulePrice> FlightSchedulePrices => Set<FlightSchedulePrice>();
    public DbSet<Seat> Seats => Set<Seat>();
    public DbSet<FlightSeat> FlightSeats => Set<FlightSeat>();
    public DbSet<Booking> Bookings => Set<Booking>();
    public DbSet<Passenger> Passengers => Set<Passenger>();
    public DbSet<Ticket> Tickets => Set<Ticket>();
    public DbSet<Payment> Payments => Set<Payment>();
    public DbSet<PaymentRefund> PaymentRefunds => Set<PaymentRefund>();
    public DbSet<BaggageOption> BaggageOptions => Set<BaggageOption>();
    public DbSet<BookingBaggage> BookingBaggage => Set<BookingBaggage>();
    public DbSet<Notification> Notifications => Set<Notification>();
    public DbSet<UploadedFile> UploadedFiles => Set<UploadedFile>();
    public DbSet<FeatureFlag> FeatureFlags => Set<FeatureFlag>();
    public DbSet<AdminLog> AdminLogs => Set<AdminLog>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();
    public DbSet<AiSearchDocument> AiSearchDocuments => Set<AiSearchDocument>();
    public DbSet<Itinerary> Itineraries => Set<Itinerary>();
    public DbSet<ItinerarySegment> ItinerarySegments => Set<ItinerarySegment>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        ConfigureEnums(modelBuilder);

        ConfigureUser(modelBuilder);
        ConfigureAirline(modelBuilder);
        ConfigureAirport(modelBuilder);
        ConfigureAircraft(modelBuilder);
        ConfigureFlight(modelBuilder);
        ConfigureFlightSchedule(modelBuilder);
        ConfigureFlightSchedulePrice(modelBuilder);
        ConfigureSeat(modelBuilder);
        ConfigureFlightSeat(modelBuilder);
        ConfigureBooking(modelBuilder);
        ConfigurePassenger(modelBuilder);
        ConfigureTicket(modelBuilder);
        ConfigurePayment(modelBuilder);
        ConfigurePaymentRefund(modelBuilder);
        ConfigureBaggageOption(modelBuilder);
        ConfigureBookingBaggage(modelBuilder);
        ConfigureNotification(modelBuilder);
        ConfigureUploadedFile(modelBuilder);
        ConfigureFeatureFlag(modelBuilder);
        ConfigureAdminLog(modelBuilder);
        ConfigureAuditLog(modelBuilder);
        ConfigureAiSearchDocument(modelBuilder);
        ConfigureItinerary(modelBuilder);
        ConfigureItinerarySegment(modelBuilder);

        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
    }

    private static void ConfigureEnums(ModelBuilder mb)
    {
        mb.HasPostgresEnum<BookingStatus>();
        mb.HasPostgresEnum<FlightScheduleStatus>();
        mb.HasPostgresEnum<SeatClass>();
        mb.HasPostgresEnum<FlightSeatStatus>();
        mb.HasPostgresEnum<TicketStatus>();
        mb.HasPostgresEnum<PaymentMethod>();
        mb.HasPostgresEnum<PaymentStatus>();
        mb.HasPostgresEnum<RefundStatus>();
    }

    private static void ConfigureUser(ModelBuilder mb) =>
        mb.Entity<User>(e =>
        {
            e.HasKey(u => u.Id);
            e.Property(u => u.Id).HasDefaultValueSql("gen_random_uuid()");
            e.Property(u => u.KeycloakUserId).HasMaxLength(255).IsRequired();
            e.HasIndex(u => u.KeycloakUserId).IsUnique();
            e.Property(u => u.FullName).HasMaxLength(200).IsRequired();
            e.Property(u => u.Email).HasMaxLength(255).IsRequired();
            e.HasIndex(u => u.Email).IsUnique();
            e.Property(u => u.PhoneNumber).HasMaxLength(30);
            e.Property(u => u.PassportNumber).HasMaxLength(50);
            e.Property(u => u.Nationality).HasMaxLength(100);
            e.Property(u => u.IsActive).HasDefaultValue(true);
            e.Property(u => u.CreatedAt).HasDefaultValueSql("now()");
            e.Property(u => u.UpdatedAt).HasDefaultValueSql("now()");
            e.HasQueryFilter(u => u.DeletedAt == null);
        });

    private static void ConfigureAirline(ModelBuilder mb) =>
        mb.Entity<Airline>(e =>
        {
            e.HasKey(a => a.Id);
            e.Property(a => a.Id).HasDefaultValueSql("gen_random_uuid()");
            e.Property(a => a.Name).HasMaxLength(150).IsRequired();
            e.Property(a => a.Code).HasMaxLength(10).IsRequired();
            e.HasIndex(a => a.Code).IsUnique();
            e.Property(a => a.Country).HasMaxLength(100).IsRequired();
            e.Property(a => a.IsActive).HasDefaultValue(true);
            e.Property(a => a.CreatedAt).HasDefaultValueSql("now()");
            e.Property(a => a.UpdatedAt).HasDefaultValueSql("now()");
            e.HasOne(a => a.LogoFile)
                .WithMany()
                .HasForeignKey(a => a.LogoFileId)
                .OnDelete(DeleteBehavior.SetNull);
            e.HasQueryFilter(a => a.DeletedAt == null);
        });

    private static void ConfigureAirport(ModelBuilder mb) =>
        mb.Entity<Airport>(e =>
        {
            e.HasKey(a => a.Id);
            e.Property(a => a.Id).HasDefaultValueSql("gen_random_uuid()");
            e.Property(a => a.Name).HasMaxLength(200).IsRequired();
            e.Property(a => a.Code).HasMaxLength(10).IsRequired();
            e.HasIndex(a => a.Code).IsUnique();
            e.Property(a => a.City).HasMaxLength(100).IsRequired();
            e.Property(a => a.Country).HasMaxLength(100).IsRequired();
            e.Property(a => a.TimeZone).HasMaxLength(60).IsRequired();
            e.Property(a => a.IsActive).HasDefaultValue(true);
            e.Property(a => a.CreatedAt).HasDefaultValueSql("now()");
            e.Property(a => a.UpdatedAt).HasDefaultValueSql("now()");
            e.HasQueryFilter(a => a.DeletedAt == null);
        });

    private static void ConfigureAircraft(ModelBuilder mb) =>
        mb.Entity<Aircraft>(e =>
        {
            e.HasKey(a => a.Id);
            e.Property(a => a.Id).HasDefaultValueSql("gen_random_uuid()");
            e.Property(a => a.Model).HasMaxLength(100).IsRequired();
            e.Property(a => a.RegistrationNumber).HasMaxLength(50).IsRequired();
            e.HasIndex(a => a.RegistrationNumber).IsUnique();
            e.Property(a => a.IsActive).HasDefaultValue(true);
            e.Property(a => a.CreatedAt).HasDefaultValueSql("now()");
            e.Property(a => a.UpdatedAt).HasDefaultValueSql("now()");
            e.HasOne(a => a.Airline)
                .WithMany(al => al.Aircrafts)
                .HasForeignKey(a => a.AirlineId)
                .OnDelete(DeleteBehavior.Restrict);
            e.HasQueryFilter(a => a.DeletedAt == null);
        });

    private static void ConfigureFlight(ModelBuilder mb) =>
        mb.Entity<Flight>(e =>
        {
            e.HasKey(f => f.Id);
            e.Property(f => f.Id).HasDefaultValueSql("gen_random_uuid()");
            e.Property(f => f.FlightNumber).HasMaxLength(20).IsRequired();
            e.Property(f => f.BasePrice).HasColumnType("numeric(10,2)");
            e.Property(f => f.IsActive).HasDefaultValue(true);
            e.Property(f => f.CreatedAt).HasDefaultValueSql("now()");
            e.Property(f => f.UpdatedAt).HasDefaultValueSql("now()");
            e.HasIndex(f => new { f.AirlineId, f.FlightNumber }).IsUnique();
            e.HasOne(f => f.Airline)
                .WithMany(a => a.Flights)
                .HasForeignKey(f => f.AirlineId)
                .OnDelete(DeleteBehavior.Restrict);
            e.HasOne(f => f.OriginAirport)
                .WithMany(a => a.OriginFlights)
                .HasForeignKey(f => f.OriginAirportId)
                .OnDelete(DeleteBehavior.Restrict);
            e.HasOne(f => f.DestinationAirport)
                .WithMany(a => a.DestinationFlights)
                .HasForeignKey(f => f.DestinationAirportId)
                .OnDelete(DeleteBehavior.Restrict);
            e.HasQueryFilter(f => f.DeletedAt == null);
        });

    private static void ConfigureFlightSchedule(ModelBuilder mb) =>
        mb.Entity<FlightSchedule>(e =>
        {
            e.HasKey(s => s.Id);
            e.Property(s => s.Id).HasDefaultValueSql("gen_random_uuid()");
            e.Property(s => s.Status)
                .HasColumnType("flight_schedule_status")
                .HasDefaultValueSql("'scheduled'::flight_schedule_status");
            e.Property(s => s.CurrentPrice).HasColumnType("numeric(10,2)");
            e.Property(s => s.Gate).HasMaxLength(20);
            e.Property(s => s.DelayReason).HasMaxLength(500);
            e.Property(s => s.CreatedAt).HasDefaultValueSql("now()");
            e.Property(s => s.UpdatedAt).HasDefaultValueSql("now()");
            e.HasIndex(s => s.DepartureTime);
            e.HasIndex(s => new { s.AircraftId, s.DepartureTime });
            e.HasOne(s => s.Flight)
                .WithMany(f => f.FlightSchedules)
                .HasForeignKey(s => s.FlightId)
                .OnDelete(DeleteBehavior.Restrict);
            e.HasOne(s => s.Aircraft)
                .WithMany(a => a.FlightSchedules)
                .HasForeignKey(s => s.AircraftId)
                .OnDelete(DeleteBehavior.Restrict);
            e.HasQueryFilter(s => s.DeletedAt == null);
        });

    private static void ConfigureFlightSchedulePrice(ModelBuilder mb) =>
        mb.Entity<FlightSchedulePrice>(e =>
        {
            e.HasKey(p => p.Id);
            e.Property(p => p.Id).HasDefaultValueSql("gen_random_uuid()");
            e.Property(p => p.SeatClass).HasColumnType("seat_class");
            e.Property(p => p.Price).HasColumnType("numeric(10,2)");
            e.Property(p => p.CreatedAt).HasDefaultValueSql("now()");
            e.Property(p => p.UpdatedAt).HasDefaultValueSql("now()");
            e.HasIndex(p => new { p.FlightScheduleId, p.SeatClass }).IsUnique();
            e.HasOne(p => p.FlightSchedule)
                .WithMany(s => s.Prices)
                .HasForeignKey(p => p.FlightScheduleId)
                .OnDelete(DeleteBehavior.Cascade);
        });

    private static void ConfigureSeat(ModelBuilder mb) =>
        mb.Entity<Seat>(e =>
        {
            e.HasKey(s => s.Id);
            e.Property(s => s.Id).HasDefaultValueSql("gen_random_uuid()");
            e.Property(s => s.SeatNumber).HasMaxLength(10).IsRequired();
            e.Property(s => s.SeatClass)
                .HasColumnType("seat_class")
                .HasDefaultValueSql("'economy'::seat_class");
            e.Property(s => s.CreatedAt).HasDefaultValueSql("now()");
            e.Property(s => s.UpdatedAt).HasDefaultValueSql("now()");
            e.HasIndex(s => new { s.AircraftId, s.SeatNumber }).IsUnique();
            e.HasOne(s => s.Aircraft)
                .WithMany(a => a.Seats)
                .HasForeignKey(s => s.AircraftId)
                .OnDelete(DeleteBehavior.Cascade);
            e.HasQueryFilter(s => s.DeletedAt == null);
        });

    private static void ConfigureFlightSeat(ModelBuilder mb) =>
        mb.Entity<FlightSeat>(e =>
        {
            e.HasKey(fs => fs.Id);
            e.Property(fs => fs.Id).HasDefaultValueSql("gen_random_uuid()");
            e.Property(fs => fs.Status)
                .HasColumnType("flight_seat_status")
                .HasDefaultValueSql("'available'::flight_seat_status");
            e.Property(fs => fs.Price).HasColumnType("numeric(10,2)");
            e.Property(fs => fs.CreatedAt).HasDefaultValueSql("now()");
            e.Property(fs => fs.UpdatedAt).HasDefaultValueSql("now()");
            e.HasIndex(fs => new { fs.FlightScheduleId, fs.SeatId }).IsUnique();
            e.HasOne(fs => fs.FlightSchedule)
                .WithMany(s => s.FlightSeats)
                .HasForeignKey(fs => fs.FlightScheduleId)
                .OnDelete(DeleteBehavior.Cascade);
            e.HasOne(fs => fs.Seat)
                .WithMany(s => s.FlightSeats)
                .HasForeignKey(fs => fs.SeatId)
                .OnDelete(DeleteBehavior.Restrict);
        });

    private static void ConfigureBooking(ModelBuilder mb) =>
        mb.Entity<Booking>(e =>
        {
            e.HasKey(b => b.Id);
            e.Property(b => b.Id).HasDefaultValueSql("gen_random_uuid()");
            e.Property(b => b.BookingReference).HasMaxLength(20).IsRequired();
            e.HasIndex(b => b.BookingReference).IsUnique();
            e.Property(b => b.Status)
                .HasColumnType("booking_status")
                .HasDefaultValueSql("'pending'::booking_status");
            e.Property(b => b.CabinClass).HasColumnType("seat_class");
            e.Property(b => b.TotalAmount).HasColumnType("numeric(10,2)");
            e.Property(b => b.CreatedAt).HasDefaultValueSql("now()");
            e.Property(b => b.UpdatedAt).HasDefaultValueSql("now()");
            e.HasOne(b => b.User)
                .WithMany(u => u.Bookings)
                .HasForeignKey(b => b.UserId)
                .OnDelete(DeleteBehavior.Restrict);
            e.HasOne(b => b.Itinerary)
                .WithMany(i => i.Bookings)
                .HasForeignKey(b => b.ItineraryId)
                .OnDelete(DeleteBehavior.SetNull);
            e.HasQueryFilter(b => b.DeletedAt == null);
        });

    private static void ConfigurePassenger(ModelBuilder mb) =>
        mb.Entity<Passenger>(e =>
        {
            e.HasKey(p => p.Id);
            e.Property(p => p.Id).HasDefaultValueSql("gen_random_uuid()");
            e.Property(p => p.FirstName).HasMaxLength(100).IsRequired();
            e.Property(p => p.LastName).HasMaxLength(100).IsRequired();
            e.Property(p => p.Gender).HasMaxLength(20);
            e.Property(p => p.PassportNumber).HasMaxLength(50);
            e.Property(p => p.Nationality).HasMaxLength(100);
            e.Property(p => p.CreatedAt).HasDefaultValueSql("now()");
            e.Property(p => p.UpdatedAt).HasDefaultValueSql("now()");
            e.HasOne(p => p.Booking)
                .WithMany(b => b.Passengers)
                .HasForeignKey(p => p.BookingId)
                .OnDelete(DeleteBehavior.Cascade);
        });

    private static void ConfigureTicket(ModelBuilder mb) =>
        mb.Entity<Ticket>(e =>
        {
            e.HasKey(t => t.Id);
            e.Property(t => t.Id).HasDefaultValueSql("gen_random_uuid()");
            e.Property(t => t.TicketNumber).HasMaxLength(30).IsRequired();
            e.HasIndex(t => t.TicketNumber).IsUnique();
            e.Property(t => t.TicketStatus)
                .HasColumnType("ticket_status")
                .HasDefaultValueSql("'issued'::ticket_status");
            e.Property(t => t.Price).HasColumnType("numeric(10,2)");
            e.Property(t => t.IssuedAt).HasDefaultValueSql("now()");
            e.Property(t => t.CreatedAt).HasDefaultValueSql("now()");
            e.Property(t => t.UpdatedAt).HasDefaultValueSql("now()");
            e.HasOne(t => t.Booking)
                .WithMany(b => b.Tickets)
                .HasForeignKey(t => t.BookingId)
                .OnDelete(DeleteBehavior.Cascade);
            e.HasOne(t => t.Passenger)
                .WithMany(p => p.Tickets)
                .HasForeignKey(t => t.PassengerId)
                .OnDelete(DeleteBehavior.Restrict);
            e.HasOne(t => t.FlightSchedule)
                .WithMany(s => s.Tickets)
                .HasForeignKey(t => t.FlightScheduleId)
                .OnDelete(DeleteBehavior.Restrict);
            e.HasOne(t => t.FlightSeat)
                .WithOne(fs => fs.Ticket!)
                .HasForeignKey<Ticket>(t => t.FlightSeatId)
                .OnDelete(DeleteBehavior.SetNull);
        });

    private static void ConfigurePayment(ModelBuilder mb) =>
        mb.Entity<Payment>(e =>
        {
            e.HasKey(p => p.Id);
            e.Property(p => p.Id).HasDefaultValueSql("gen_random_uuid()");
            e.Property(p => p.Amount).HasColumnType("numeric(10,2)");
            e.Property(p => p.PaymentMethod).HasColumnType("payment_method");
            e.Property(p => p.PaymentStatus)
                .HasColumnType("payment_status")
                .HasDefaultValueSql("'pending'::payment_status");
            e.Property(p => p.TransactionId).HasMaxLength(100);
            e.Property(p => p.CreatedAt).HasDefaultValueSql("now()");
            e.Property(p => p.UpdatedAt).HasDefaultValueSql("now()");
            e.HasOne(p => p.Booking)
                .WithMany(b => b.Payments)
                .HasForeignKey(p => p.BookingId)
                .OnDelete(DeleteBehavior.Cascade);
        });

    private static void ConfigurePaymentRefund(ModelBuilder mb) =>
        mb.Entity<PaymentRefund>(e =>
        {
            e.HasKey(r => r.Id);
            e.Property(r => r.Id).HasDefaultValueSql("gen_random_uuid()");
            e.Property(r => r.Amount).HasColumnType("numeric(10,2)");
            e.Property(r => r.Reason).HasMaxLength(500).IsRequired();
            e.Property(r => r.RefundStatus)
                .HasColumnType("refund_status")
                .HasDefaultValueSql("'pending'::refund_status");
            e.Property(r => r.CreatedAt).HasDefaultValueSql("now()");
            e.Property(r => r.UpdatedAt).HasDefaultValueSql("now()");
            e.HasOne(r => r.Payment)
                .WithMany(p => p.Refunds)
                .HasForeignKey(r => r.PaymentId)
                .OnDelete(DeleteBehavior.Cascade);
        });

    private static void ConfigureBaggageOption(ModelBuilder mb) =>
        mb.Entity<BaggageOption>(e =>
        {
            e.HasKey(b => b.Id);
            e.Property(b => b.Id).HasDefaultValueSql("gen_random_uuid()");
            e.Property(b => b.Name).HasMaxLength(100).IsRequired();
            e.Property(b => b.WeightKg).HasColumnType("numeric(5,2)");
            e.Property(b => b.Price).HasColumnType("numeric(10,2)");
            e.Property(b => b.Description).HasMaxLength(500);
            e.Property(b => b.IsActive).HasDefaultValue(true);
            e.Property(b => b.CreatedAt).HasDefaultValueSql("now()");
            e.Property(b => b.UpdatedAt).HasDefaultValueSql("now()");
            e.HasQueryFilter(b => b.DeletedAt == null);
        });

    private static void ConfigureBookingBaggage(ModelBuilder mb) =>
        mb.Entity<BookingBaggage>(e =>
        {
            e.HasKey(bb => bb.Id);
            e.Property(bb => bb.Id).HasDefaultValueSql("gen_random_uuid()");
            e.Property(bb => bb.Quantity).HasDefaultValue(1);
            e.Property(bb => bb.CreatedAt).HasDefaultValueSql("now()");
            e.Property(bb => bb.UpdatedAt).HasDefaultValueSql("now()");
            e.HasOne(bb => bb.Booking)
                .WithMany(b => b.BookingBaggage)
                .HasForeignKey(bb => bb.BookingId)
                .OnDelete(DeleteBehavior.Cascade);
            e.HasOne(bb => bb.Passenger)
                .WithMany(p => p.BookingBaggage)
                .HasForeignKey(bb => bb.PassengerId)
                .OnDelete(DeleteBehavior.Cascade);
            e.HasOne(bb => bb.BaggageOption)
                .WithMany(bo => bo.BookingBaggage)
                .HasForeignKey(bb => bb.BaggageOptionId)
                .OnDelete(DeleteBehavior.Restrict);
        });

    private static void ConfigureNotification(ModelBuilder mb) =>
        mb.Entity<Notification>(e =>
        {
            e.HasKey(n => n.Id);
            e.Property(n => n.Id).HasDefaultValueSql("gen_random_uuid()");
            e.Property(n => n.Title).HasMaxLength(200).IsRequired();
            e.Property(n => n.Type).HasMaxLength(50).IsRequired();
            e.Property(n => n.RelatedEntityName).HasMaxLength(100);
            e.Property(n => n.IsRead).HasDefaultValue(false);
            e.Property(n => n.CreatedAt).HasDefaultValueSql("now()");
            e.HasIndex(n => new { n.UserId, n.IsRead });
            e.HasOne(n => n.User)
                .WithMany(u => u.Notifications)
                .HasForeignKey(n => n.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

    private static void ConfigureUploadedFile(ModelBuilder mb) =>
        mb.Entity<UploadedFile>(e =>
        {
            e.HasKey(f => f.Id);
            e.Property(f => f.Id).HasDefaultValueSql("gen_random_uuid()");
            e.Property(f => f.FileName).HasMaxLength(255).IsRequired();
            e.Property(f => f.OriginalFileName).HasMaxLength(255).IsRequired();
            e.Property(f => f.ContentType).HasMaxLength(100).IsRequired();
            e.Property(f => f.StoragePath).HasMaxLength(500).IsRequired();
            e.Property(f => f.RelatedEntityName).HasMaxLength(100);
            e.Property(f => f.CreatedAt).HasDefaultValueSql("now()");
            e.HasOne(f => f.UploadedBy)
                .WithMany(u => u.UploadedFiles)
                .HasForeignKey(f => f.UploadedByUserId)
                .OnDelete(DeleteBehavior.Restrict);
        });

    private static void ConfigureFeatureFlag(ModelBuilder mb) =>
        mb.Entity<FeatureFlag>(e =>
        {
            e.HasKey(f => f.Id);
            e.Property(f => f.Id).HasDefaultValueSql("gen_random_uuid()");
            e.Property(f => f.Key).HasMaxLength(100).IsRequired();
            e.HasIndex(f => f.Key).IsUnique();
            e.Property(f => f.Name).HasMaxLength(200).IsRequired();
            e.Property(f => f.Description).HasMaxLength(500);
            e.Property(f => f.CreatedAt).HasDefaultValueSql("now()");
            e.Property(f => f.UpdatedAt).HasDefaultValueSql("now()");
        });

    private static void ConfigureAdminLog(ModelBuilder mb) =>
        mb.Entity<AdminLog>(e =>
        {
            e.HasKey(l => l.Id);
            e.Property(l => l.Id).HasDefaultValueSql("gen_random_uuid()");
            e.Property(l => l.Action).HasMaxLength(100).IsRequired();
            e.Property(l => l.EntityName).HasMaxLength(100).IsRequired();
            e.Property(l => l.Description).HasMaxLength(1000);
            e.Property(l => l.CreatedAt).HasDefaultValueSql("now()");
            e.HasIndex(l => l.CreatedAt);
            e.HasOne(l => l.AdminUser)
                .WithMany()
                .HasForeignKey(l => l.AdminUserId)
                .OnDelete(DeleteBehavior.Restrict);
        });

    private static void ConfigureAuditLog(ModelBuilder mb) =>
        mb.Entity<AuditLog>(e =>
        {
            e.HasKey(l => l.Id);
            e.Property(l => l.Id).HasDefaultValueSql("gen_random_uuid()");
            e.Property(l => l.EntityName).HasMaxLength(100).IsRequired();
            e.Property(l => l.Action).HasMaxLength(50).IsRequired();
            e.Property(l => l.OldValues).HasColumnType("jsonb");
            e.Property(l => l.NewValues).HasColumnType("jsonb");
            e.Property(l => l.CreatedAt).HasDefaultValueSql("now()");
            e.HasIndex(l => new { l.EntityName, l.EntityId });
            e.HasOne(l => l.User)
                .WithMany()
                .HasForeignKey(l => l.UserId)
                .OnDelete(DeleteBehavior.SetNull);
        });

    private static void ConfigureAiSearchDocument(ModelBuilder mb) =>
        mb.Entity<AiSearchDocument>(e =>
        {
            e.HasKey(d => d.Id);
            e.Property(d => d.Id).HasDefaultValueSql("gen_random_uuid()");
            e.Property(d => d.Title).HasMaxLength(300).IsRequired();
            e.Property(d => d.Source).HasMaxLength(200).IsRequired();
            e.Property(d => d.Embedding).HasColumnType("real[]");
            e.Property(d => d.CreatedAt).HasDefaultValueSql("now()");
            e.Property(d => d.UpdatedAt).HasDefaultValueSql("now()");
        });

    private static void ConfigureItinerary(ModelBuilder mb) =>
        mb.Entity<Itinerary>(e =>
        {
            e.HasKey(i => i.Id);
            e.Property(i => i.Id).HasDefaultValueSql("gen_random_uuid()");
            e.Property(i => i.TotalPrice).HasColumnType("numeric(10,2)");
            e.Property(i => i.IsActive).HasDefaultValue(true);
            e.Property(i => i.CreatedAt).HasDefaultValueSql("now()");
            e.Property(i => i.UpdatedAt).HasDefaultValueSql("now()");
            e.HasIndex(i => new { i.OriginAirportId, i.DestinationAirportId, i.DepartureTime });
            e.HasOne(i => i.OriginAirport)
                .WithMany()
                .HasForeignKey(i => i.OriginAirportId)
                .OnDelete(DeleteBehavior.Restrict);
            e.HasOne(i => i.DestinationAirport)
                .WithMany()
                .HasForeignKey(i => i.DestinationAirportId)
                .OnDelete(DeleteBehavior.Restrict);
            e.HasQueryFilter(i => i.DeletedAt == null);
        });

    private static void ConfigureItinerarySegment(ModelBuilder mb) =>
        mb.Entity<ItinerarySegment>(e =>
        {
            e.HasKey(s => s.Id);
            e.Property(s => s.Id).HasDefaultValueSql("gen_random_uuid()");
            e.Property(s => s.CreatedAt).HasDefaultValueSql("now()");
            e.Property(s => s.UpdatedAt).HasDefaultValueSql("now()");
            e.HasIndex(s => new { s.ItineraryId, s.SegmentOrder }).IsUnique();
            e.HasOne(s => s.Itinerary)
                .WithMany(i => i.Segments)
                .HasForeignKey(s => s.ItineraryId)
                .OnDelete(DeleteBehavior.Cascade);
            e.HasOne(s => s.FlightSchedule)
                .WithMany(fs => fs.ItinerarySegments)
                .HasForeignKey(s => s.FlightScheduleId)
                .OnDelete(DeleteBehavior.Restrict);
        });
}
