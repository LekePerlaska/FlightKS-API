using FlightKS.Models.Entities;
using Microsoft.EntityFrameworkCore;

namespace FlightKS.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<User> Users => Set<User>();
    public DbSet<Flight> Flights => Set<Flight>();
    public DbSet<Booking> Bookings => Set<Booking>();
    public DbSet<BookingFlight> BookingFlights => Set<BookingFlight>();
    public DbSet<Passenger> Passengers => Set<Passenger>();
    public DbSet<Payment> Payments => Set<Payment>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<BookingFlight>().HasKey(bf => new { bf.BookingId, bf.FlightId });

        // Picks up all IEntityTypeConfiguration<T> classes in this assembly
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
    }
}
