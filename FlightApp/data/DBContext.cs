using FlightKS.Models.Entities;
using Microsoft.EntityFrameworkCore;

namespace FlightKS.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<User> Users => Set<User>();
    public DbSet<Airport> Airports => Set<Airport>();
    public DbSet<Airline> Airlines => Set<Airline>();
    public DbSet<Aircraft> Aircraft => Set<Aircraft>();
    public DbSet<Flight> Flights => Set<Flight>();
    public DbSet<FlightFare> FlightFares => Set<FlightFare>();
    public DbSet<BaggageOption> BaggageOptions => Set<BaggageOption>();
    public DbSet<Booking> Bookings => Set<Booking>();
    public DbSet<BookingSegment> BookingSegments => Set<BookingSegment>();
    public DbSet<Passenger> Passengers => Set<Passenger>();
    public DbSet<PassengerSegment> PassengerSegments => Set<PassengerSegment>();
    public DbSet<PassengerBaggage> PassengerBaggages => Set<PassengerBaggage>();
    public DbSet<Payment> Payments => Set<Payment>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Picks up all IEntityTypeConfiguration<T> classes in this assembly
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
    }
}
