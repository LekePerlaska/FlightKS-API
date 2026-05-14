using FlightKS.Enums;
using FlightKS.Models.Entities;
using Microsoft.EntityFrameworkCore;

namespace FlightKS.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<Airport> Airports => Set<Airport>();
    public DbSet<User> Users => Set<User>();
    public DbSet<UserPassport> UserPassports => Set<UserPassport>();
    public DbSet<UserPreferences> UserPreferences => Set<UserPreferences>();
    public DbSet<UserLoyalty> UserLoyalty => Set<UserLoyalty>();
    public DbSet<Flight> Flights => Set<Flight>();
    public DbSet<FlightPrice> FlightPrices => Set<FlightPrice>();
    public DbSet<Booking> Bookings => Set<Booking>();
    public DbSet<BookingPassenger> BookingPassengers => Set<BookingPassenger>();
    public DbSet<SavedTraveler> SavedTravelers => Set<SavedTraveler>();
    public DbSet<PriceAlert> PriceAlerts => Set<PriceAlert>();
    public DbSet<Notification> Notifications => Set<Notification>();
    public DbSet<SavedFlight> SavedFlights => Set<SavedFlight>();
    public DbSet<SearchHistory> SearchHistory => Set<SearchHistory>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        ConfigureEnums(modelBuilder);
        ConfigureAirport(modelBuilder);
        ConfigureUser(modelBuilder);
        ConfigureFlight(modelBuilder);
        ConfigureBooking(modelBuilder);
        ConfigureSavedTraveler(modelBuilder);
        ConfigurePriceAlert(modelBuilder);
        ConfigureNotification(modelBuilder);
        ConfigureSavedFlight(modelBuilder);
        ConfigureSearchHistory(modelBuilder);

        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
    }

    private static void ConfigureEnums(ModelBuilder mb)
    {
        mb.HasPostgresEnum<CabinClass>();
        mb.HasPostgresEnum<TripType>();
        mb.HasPostgresEnum<BookingStatus>();
        mb.HasPostgresEnum<NotifType>();
        mb.HasPostgresEnum<SeatSide>();
        mb.HasPostgresEnum<PassengerType>();
    }

    private static void ConfigureAirport(ModelBuilder mb)
    {
        mb.Entity<Airport>(e =>
        {
            e.HasKey(a => a.Code);
            e.Property(a => a.Code).HasColumnType("char(3)");
            e.Property(a => a.City).HasMaxLength(100).IsRequired();
            e.Property(a => a.Country).HasMaxLength(100).IsRequired();
            e.Property(a => a.Name).HasMaxLength(200);
            e.Property(a => a.Timezone).HasMaxLength(60);
        });
    }

    private static void ConfigureUser(ModelBuilder mb)
    {
        mb.Entity<User>(e =>
        {
            e.HasKey(u => u.Id);
            e.Property(u => u.Id).HasDefaultValueSql("gen_random_uuid()");
            e.Property(u => u.Email).HasMaxLength(255).IsRequired();
            e.HasIndex(u => u.Email).IsUnique();
            e.Property(u => u.PasswordHash).HasMaxLength(255).IsRequired();
            e.Property(u => u.FirstName).HasMaxLength(100).IsRequired();
            e.Property(u => u.LastName).HasMaxLength(100).IsRequired();
            e.Property(u => u.Phone).HasMaxLength(30);
            e.Property(u => u.Nationality).HasMaxLength(100);
            e.Property(u => u.CreatedAt).HasDefaultValueSql("now()");
            e.Property(u => u.UpdatedAt).HasDefaultValueSql("now()");
        });

        mb.Entity<UserPassport>(e =>
        {
            e.HasKey(p => p.UserId);
            e.Property(p => p.PassportNumber).HasMaxLength(50).IsRequired();
            e.Property(p => p.Nationality).HasMaxLength(100).IsRequired();
            e.HasOne(p => p.User)
                .WithOne(u => u.Passport)
                .HasForeignKey<UserPassport>(p => p.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        mb.Entity<UserPreferences>(e =>
        {
            e.HasKey(p => p.UserId);
            e.Property(p => p.PreferredCabin).HasColumnType("cabin_class");
            e.Property(p => p.PreferredSeat).HasColumnType("seat_side");
            e.Property(p => p.PreferredMeal).HasMaxLength(50);
            e.Property(p => p.PreferredCurrency).HasColumnType("char(3)");
            e.Property(p => p.EmailNotifications).HasDefaultValue(true);
            e.Property(p => p.SmsNotifications).HasDefaultValue(false);
            e.HasOne(p => p.User)
                .WithOne(u => u.Preferences)
                .HasForeignKey<UserPreferences>(p => p.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        mb.Entity<UserLoyalty>(e =>
        {
            e.HasKey(l => l.UserId);
            e.Property(l => l.FrequentFlyerProgram).HasMaxLength(100).IsRequired();
            e.Property(l => l.FrequentFlyerNumber).HasMaxLength(50).IsRequired();
            e.HasOne(l => l.User)
                .WithOne(u => u.Loyalty)
                .HasForeignKey<UserLoyalty>(l => l.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }

    private static void ConfigureFlight(ModelBuilder mb)
    {
        mb.Entity<Flight>(e =>
        {
            e.HasKey(f => f.Id);
            e.Property(f => f.Id).HasMaxLength(50);
            e.Property(f => f.Origin).HasColumnType("char(3)").IsRequired();
            e.Property(f => f.Destination).HasColumnType("char(3)").IsRequired();
            e.Property(f => f.Airline).HasMaxLength(100).IsRequired();
            e.Property(f => f.FlightNumber).HasMaxLength(20).IsRequired();
            e.Property(f => f.Currency).HasColumnType("char(3)").HasDefaultValue("USD");
            e.Property(f => f.Stops).HasDefaultValue((short)0);
            e.Property(f => f.BaggageIncluded).HasDefaultValue(false);
            e.Property(f => f.Refundable).HasDefaultValue(false);
            e.Property(f => f.CreatedAt).HasDefaultValueSql("now()");

            e.HasOne(f => f.OriginAirport)
                .WithMany()
                .HasForeignKey(f => f.Origin)
                .OnDelete(DeleteBehavior.Restrict);

            e.HasOne(f => f.DestinationAirport)
                .WithMany()
                .HasForeignKey(f => f.Destination)
                .OnDelete(DeleteBehavior.Restrict);

            e.HasMany(f => f.Prices)
                .WithOne(p => p.Flight)
                .HasForeignKey(p => p.FlightId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        mb.Entity<FlightPrice>(e =>
        {
            e.HasKey(p => new { p.FlightId, p.Cabin });
            e.Property(p => p.FlightId).HasMaxLength(50);
            e.Property(p => p.Cabin).HasColumnType("cabin_class");
            e.Property(p => p.Price).HasColumnType("numeric(10,2)");
        });
    }

    private static void ConfigureBooking(ModelBuilder mb)
    {
        mb.Entity<Booking>(e =>
        {
            e.HasKey(b => b.Id);
            e.Property(b => b.Id).HasDefaultValueSql("gen_random_uuid()");
            e.Property(b => b.Reference).HasMaxLength(20).IsRequired();
            e.HasIndex(b => b.Reference).IsUnique();
            e.Property(b => b.FlightId).HasMaxLength(50).IsRequired();
            e.Property(b => b.Cabin).HasColumnType("cabin_class");
            e.Property(b => b.TripType).HasColumnType("trip_type");
            e.Property(b => b.Status).HasColumnType("booking_status").HasDefaultValueSql("'upcoming'::booking_status");
            e.Property(b => b.PassengerCount).HasDefaultValue((short)1);
            e.Property(b => b.BaseFare).HasColumnType("numeric(10,2)");
            e.Property(b => b.TaxesFees).HasColumnType("numeric(10,2)");
            e.Property(b => b.TotalPrice).HasColumnType("numeric(10,2)");
            e.Property(b => b.BookedAt).HasDefaultValueSql("now()");
            e.Property(b => b.UpdatedAt).HasDefaultValueSql("now()");

            e.HasOne(b => b.User)
                .WithMany(u => u.Bookings)
                .HasForeignKey(b => b.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            e.HasOne(b => b.Flight)
                .WithMany()
                .HasForeignKey(b => b.FlightId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        mb.Entity<BookingPassenger>(e =>
        {
            e.HasKey(p => p.Id);
            e.Property(p => p.Id).HasDefaultValueSql("gen_random_uuid()");
            e.Property(p => p.FirstName).HasMaxLength(100).IsRequired();
            e.Property(p => p.LastName).HasMaxLength(100).IsRequired();
            e.Property(p => p.Nationality).HasMaxLength(100).IsRequired();
            e.Property(p => p.PassportNumber).HasMaxLength(50).IsRequired();
            e.Property(p => p.Email).HasMaxLength(255).IsRequired();
            e.Property(p => p.Phone).HasMaxLength(30);
            e.Property(p => p.SeatNumber).HasMaxLength(10);
            e.Property(p => p.FrequentFlyerNumber).HasMaxLength(50);
            e.Property(p => p.Fare).HasColumnType("numeric(10,2)");
            e.Property(p => p.PassengerType).HasColumnType("passenger_type").HasDefaultValueSql("'adult'::passenger_type");

            e.HasOne(p => p.Booking)
                .WithMany(b => b.Passengers)
                .HasForeignKey(p => p.BookingId)
                .OnDelete(DeleteBehavior.Cascade);

            e.HasIndex(p => new { p.BookingId, p.SeatNumber })
                .IsUnique()
                .HasFilter("seat_number IS NOT NULL");
        });
    }

    private static void ConfigureSavedTraveler(ModelBuilder mb)
    {
        mb.Entity<SavedTraveler>(e =>
        {
            e.HasKey(t => t.Id);
            e.Property(t => t.Id).HasDefaultValueSql("gen_random_uuid()");
            e.Property(t => t.FirstName).HasMaxLength(100).IsRequired();
            e.Property(t => t.LastName).HasMaxLength(100).IsRequired();
            e.Property(t => t.Email).HasMaxLength(255).IsRequired();
            e.Property(t => t.Phone).HasMaxLength(30).IsRequired();
            e.Property(t => t.Nationality).HasMaxLength(100);
            e.Property(t => t.PassportNumber).HasMaxLength(50);
            e.Property(t => t.IsDefault).HasDefaultValue(false);
            e.Property(t => t.CreatedAt).HasDefaultValueSql("now()");

            e.HasOne(t => t.User)
                .WithMany(u => u.SavedTravelers)
                .HasForeignKey(t => t.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }

    private static void ConfigurePriceAlert(ModelBuilder mb)
    {
        mb.Entity<PriceAlert>(e =>
        {
            e.HasKey(a => a.Id);
            e.Property(a => a.Id).HasDefaultValueSql("gen_random_uuid()");
            e.Property(a => a.Origin).HasColumnType("char(3)").IsRequired();
            e.Property(a => a.Destination).HasColumnType("char(3)").IsRequired();
            e.Property(a => a.Cabin).HasColumnType("cabin_class");
            e.Property(a => a.TargetPrice).HasColumnType("numeric(10,2)");
            e.Property(a => a.CurrentPrice).HasColumnType("numeric(10,2)");
            e.Property(a => a.Active).HasDefaultValue(true);
            e.Property(a => a.CreatedAt).HasDefaultValueSql("now()");

            e.HasOne(a => a.User)
                .WithMany(u => u.PriceAlerts)
                .HasForeignKey(a => a.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            e.HasOne(a => a.OriginAirport)
                .WithMany()
                .HasForeignKey(a => a.Origin)
                .OnDelete(DeleteBehavior.Restrict);

            e.HasOne(a => a.DestinationAirport)
                .WithMany()
                .HasForeignKey(a => a.Destination)
                .OnDelete(DeleteBehavior.Restrict);
        });
    }

    private static void ConfigureNotification(ModelBuilder mb)
    {
        mb.Entity<Notification>(e =>
        {
            e.HasKey(n => n.Id);
            e.Property(n => n.Id).HasDefaultValueSql("gen_random_uuid()");
            e.Property(n => n.Type).HasColumnType("notif_type");
            e.Property(n => n.Title).HasMaxLength(200).IsRequired();
            e.Property(n => n.Read).HasDefaultValue(false);
            e.Property(n => n.CreatedAt).HasDefaultValueSql("now()");

            e.HasOne(n => n.User)
                .WithMany(u => u.Notifications)
                .HasForeignKey(n => n.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }

    private static void ConfigureSavedFlight(ModelBuilder mb)
    {
        mb.Entity<SavedFlight>(e =>
        {
            e.HasKey(s => s.Id);
            e.Property(s => s.Id).HasDefaultValueSql("gen_random_uuid()");
            e.Property(s => s.FlightId).HasMaxLength(50).IsRequired();
            e.Property(s => s.Cabin).HasColumnType("cabin_class");
            e.Property(s => s.Adults).HasDefaultValue((short)1);
            e.Property(s => s.PriceAtSave).HasColumnType("numeric(10,2)");
            e.Property(s => s.SavedAt).HasDefaultValueSql("now()");

            e.HasOne(s => s.User)
                .WithMany(u => u.SavedFlights)
                .HasForeignKey(s => s.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            e.HasOne(s => s.Flight)
                .WithMany()
                .HasForeignKey(s => s.FlightId)
                .OnDelete(DeleteBehavior.Restrict);

            e.HasIndex(s => new { s.UserId, s.FlightId, s.Cabin, s.DepartDate }).IsUnique();
        });
    }

    private static void ConfigureSearchHistory(ModelBuilder mb)
    {
        mb.Entity<SearchHistory>(e =>
        {
            e.HasKey(s => s.Id);
            e.Property(s => s.Id).HasDefaultValueSql("gen_random_uuid()");
            e.Property(s => s.Origin).HasColumnType("char(3)").IsRequired();
            e.Property(s => s.Destination).HasColumnType("char(3)").IsRequired();
            e.Property(s => s.TripType).HasColumnType("trip_type");
            e.Property(s => s.Cabin).HasColumnType("cabin_class");
            e.Property(s => s.Adults).HasDefaultValue((short)1);
            e.Property(s => s.SearchedAt).HasDefaultValueSql("now()");

            e.HasOne(s => s.User)
                .WithMany(u => u.SearchHistory)
                .HasForeignKey(s => s.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            e.HasOne(s => s.OriginAirport)
                .WithMany()
                .HasForeignKey(s => s.Origin)
                .OnDelete(DeleteBehavior.Restrict);

            e.HasOne(s => s.DestinationAirport)
                .WithMany()
                .HasForeignKey(s => s.Destination)
                .OnDelete(DeleteBehavior.Restrict);
        });
    }
}
