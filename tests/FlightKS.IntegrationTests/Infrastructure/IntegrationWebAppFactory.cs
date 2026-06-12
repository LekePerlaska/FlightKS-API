using FlightKS.Data;
using FlightKS.Enums;
using FlightKS.Models.Entities;
using FlightKS.Services.Interfaces;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Npgsql;
using Npgsql.EntityFrameworkCore.PostgreSQL.Infrastructure;
using Respawn;
using Testcontainers.PostgreSql;

namespace FlightKS.IntegrationTests.Infrastructure;

/// <summary>
/// Boots the full ASP.NET Core app against a real Postgres container.
/// Shared across all tests in the Integration collection via ICollectionFixture —
/// migrations run once on first boot; Respawn truncates tables between tests.
/// </summary>
public sealed class IntegrationWebAppFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    private readonly PostgreSqlContainer _db = new PostgreSqlBuilder()
        .WithImage("postgres:16-alpine")
        .Build();

    private NpgsqlDataSource _dataSource = null!;
    private Respawner _respawner = null!;

    // ── IAsyncLifetime ────────────────────────────────────────────────────

    public async Task InitializeAsync()
    {
        await _db.StartAsync();
        _dataSource = BuildDataSource(_db.GetConnectionString());

        // Trigger host creation → Program.cs runs MigrateAsync against the container.
        _ = Server;

        // Respawn can now detect tables (schema exists after migrations).
        await using var conn = await _dataSource.OpenConnectionAsync();
        _respawner = await Respawner.CreateAsync(conn, new RespawnerOptions
        {
            DbAdapter = DbAdapter.Postgres
        });
    }

    public new async Task DisposeAsync()
    {
        if (_dataSource is not null)
            await _dataSource.DisposeAsync();
        await _db.DisposeAsync();
        await base.DisposeAsync();
    }

    // ── Per-test helpers ──────────────────────────────────────────────────

    public async Task ResetAsync()
    {
        await using var conn = await _dataSource.OpenConnectionAsync();
        await _respawner.ResetAsync(conn);
    }

    /// <summary>Creates an AppDbContext that talks to the test container.</summary>
    public AppDbContext CreateDbContext()
    {
        var opts = new DbContextOptionsBuilder<AppDbContext>()
            .UseNpgsql(_dataSource, ConfigureNpgsql)
            .UseSnakeCaseNamingConvention()
            .Options;
        return new AppDbContext(opts);
    }

    /// <summary>
    /// Seeds the canonical test user that TestAuthHandler authenticates as.
    /// Must be called after ResetAsync() since Respawn deletes all rows.
    /// </summary>
    public async Task<Guid> SeedTestUserAsync()
    {
        await using var db = CreateDbContext();
        var user = new User
        {
            KeycloakUserId = TestAuthHandler.TestKeycloakId,
            Email = TestAuthHandler.TestEmail,
            FullName = TestAuthHandler.TestFullName
        };
        db.Users.Add(user);
        await db.SaveChangesAsync();
        return user.Id;
    }

    /// <summary>
    /// Returns an HttpClient that auto-authenticates as the test user.
    /// Pass roles to override the default ["User"] set.
    /// </summary>
    public HttpClient CreateAuthenticatedClient(params string[] roles)
    {
        var client = CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });
        // TestAuthHandler only activates when an Authorization header is present.
        client.DefaultRequestHeaders.Authorization = TestAuthHandler.BearerHeader();
        if (roles.Length > 0)
            client.DefaultRequestHeaders.Add(TestAuthHandler.RolesHeader, string.Join(",", roles));
        return client;
    }

    /// <summary>Returns an HttpClient with no authentication headers (anonymous).</summary>
    public HttpClient CreateAnonymousClient() =>
        CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

    // ── WebApplicationFactory overrides ──────────────────────────────────

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        // Point the app at the test Postgres container.
        // Program.cs reads ConnectionStrings:DefaultConnection to run migrations and
        // build the NpgsqlDataSource — this override wins over appsettings.json.
        builder.UseSetting(
            "ConnectionStrings:DefaultConnection",
            _db.GetConnectionString());

        // Silence Loki in tests — the sink will fail-quietly anyway, but this
        // suppresses the noisy startup warning about the unreachable endpoint.
        builder.UseSetting("Loki:Uri", "http://localhost:1");

        builder.ConfigureTestServices(services =>
        {
            // Replace Keycloak JWT Bearer with the test auth scheme.
            services.AddAuthentication(TestAuthHandler.SchemeName)
                    .AddScheme<AuthenticationSchemeOptions, TestAuthHandler>(
                        TestAuthHandler.SchemeName, _ => { });

            // Make the default authenticate/challenge/forbid scheme the test one.
            services.Configure<AuthenticationOptions>(opts =>
            {
                opts.DefaultAuthenticateScheme = TestAuthHandler.SchemeName;
                opts.DefaultChallengeScheme    = TestAuthHandler.SchemeName;
                opts.DefaultForbidScheme       = TestAuthHandler.SchemeName;
            });

            // Replace the real KeycloakService (requires a live Keycloak) with a no-op stub.
            // Admin endpoints call it to fetch user roles; the stub returns ["User"] for everyone.
            services.RemoveAll<IKeycloakService>();
            services.AddSingleton<IKeycloakService, StubKeycloakService>();
        });
    }

    // ── Npgsql helpers ────────────────────────────────────────────────────

    private static NpgsqlDataSource BuildDataSource(string connectionString)
    {
        var b = new NpgsqlDataSourceBuilder(connectionString);
        MapEnums(b);
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

    private static void MapEnums(NpgsqlDataSourceBuilder b)
    {
        b.MapEnum<BookingStatus>("booking_status");
        b.MapEnum<FlightScheduleStatus>("flight_schedule_status");
        b.MapEnum<SeatClass>("seat_class");
        b.MapEnum<FlightSeatStatus>("flight_seat_status");
        b.MapEnum<TicketStatus>("ticket_status");
        b.MapEnum<PaymentMethod>("payment_method");
        b.MapEnum<PaymentStatus>("payment_status");
        b.MapEnum<RefundStatus>("refund_status");
    }
}
