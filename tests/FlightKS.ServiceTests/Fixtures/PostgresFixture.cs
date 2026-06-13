using FlightKS.Data;
using FlightKS.Enums;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using Npgsql.EntityFrameworkCore.PostgreSQL.Infrastructure;
using Respawn;
using Testcontainers.PostgreSql;

namespace FlightKS.ServiceTests.Fixtures;

public sealed class PostgresFixture : IAsyncLifetime
{
    private readonly PostgreSqlContainer _container = new PostgreSqlBuilder()
        .WithImage("postgres:16-alpine")
        .Build();

    private NpgsqlDataSource _dataSource = null!;
    private Respawner _respawner = null!;

    public async Task InitializeAsync()
    {
        await _container.StartAsync();
        _dataSource = BuildDataSource(_container.GetConnectionString());

        await using var ctx = CreateContext();
        await ctx.Database.MigrateAsync();

        await using var conn = await _dataSource.OpenConnectionAsync();
        _respawner = await Respawner.CreateAsync(conn, new RespawnerOptions
        {
            DbAdapter = DbAdapter.Postgres
        });
    }

    public async Task DisposeAsync()
    {
        if (_dataSource is not null)
            await _dataSource.DisposeAsync();
        await _container.DisposeAsync();
    }

    public AppDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseNpgsql(_dataSource, ConfigureNpgsql)
            .UseSnakeCaseNamingConvention()
            .Options;
        return new AppDbContext(options);
    }

    public async Task ResetAsync()
    {
        await using var conn = await _dataSource.OpenConnectionAsync();
        await _respawner.ResetAsync(conn);
    }

    private static NpgsqlDataSource BuildDataSource(string connectionString)
    {
        var b = new NpgsqlDataSourceBuilder(connectionString);
        b.MapEnum<BookingStatus>("booking_status");
        b.MapEnum<FlightScheduleStatus>("flight_schedule_status");
        b.MapEnum<SeatClass>("seat_class");
        b.MapEnum<FlightSeatStatus>("flight_seat_status");
        b.MapEnum<TicketStatus>("ticket_status");
        b.MapEnum<PaymentMethod>("payment_method");
        b.MapEnum<PaymentStatus>("payment_status");
        b.MapEnum<RefundStatus>("refund_status");
        return b.Build();
    }

    private static void ConfigureNpgsql(NpgsqlDbContextOptionsBuilder npg)
    {
        npg.MapEnum<BookingStatus>("booking_status");
        npg.MapEnum<FlightScheduleStatus>("flight_schedule_status");
        npg.MapEnum<SeatClass>("seat_class");
        npg.MapEnum<FlightSeatStatus>("flight_seat_status");
        npg.MapEnum<TicketStatus>("ticket_status");
        npg.MapEnum<PaymentMethod>("payment_method");
        npg.MapEnum<PaymentStatus>("payment_status");
        npg.MapEnum<RefundStatus>("refund_status");
    }
}
