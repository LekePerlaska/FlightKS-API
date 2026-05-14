using System.Text.Json;
using System.Text.Json.Serialization;
using FlightKS.Data;
using FlightKS.Endpoints;
using FlightKS.Enums;
using FlightKS.Services;
using FlightKS.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using Scalar.AspNetCore;
using Serilog;
using Serilog.Sinks.Grafana.Loki;

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseSerilog((ctx, cfg) =>
    cfg.ReadFrom.Configuration(ctx.Configuration)
       .Enrich.FromLogContext()
       .WriteTo.Console()
       .WriteTo.GrafanaLoki(
           ctx.Configuration["Loki:Uri"] ?? "http://localhost:3100",
           labels: [new LokiLabel { Key = "app", Value = "flightks-api" }]
       )
);

builder.Services.AddOpenApi();

builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.Converters.Add(new JsonStringEnumConverter(JsonNamingPolicy.SnakeCaseLower));
});

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")!;

// Migrate first with a vanilla data source so the PG enum types exist
// before the typed data source probes them on its first connection.
await using (var bootCtx = new AppDbContext(new DbContextOptionsBuilder<AppDbContext>()
    .UseNpgsql(connectionString)
    .UseSnakeCaseNamingConvention()
    .Options))
{
    await bootCtx.Database.MigrateAsync();
}

var dataSourceBuilder = new NpgsqlDataSourceBuilder(connectionString);
dataSourceBuilder.MapEnum<CabinClass>("cabin_class");
dataSourceBuilder.MapEnum<TripType>("trip_type");
dataSourceBuilder.MapEnum<BookingStatus>("booking_status");
dataSourceBuilder.MapEnum<NotifType>("notif_type");
dataSourceBuilder.MapEnum<SeatSide>("seat_side");
dataSourceBuilder.MapEnum<PassengerType>("passenger_type");
var dataSource = dataSourceBuilder.Build();

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(dataSource, npg =>
    {
        npg.MapEnum<CabinClass>("cabin_class");
        npg.MapEnum<TripType>("trip_type");
        npg.MapEnum<BookingStatus>("booking_status");
        npg.MapEnum<NotifType>("notif_type");
        npg.MapEnum<SeatSide>("seat_side");
        npg.MapEnum<PassengerType>("passenger_type");
    }).UseSnakeCaseNamingConvention());

builder.Services.AddSingleton<IPasswordHasher, Pbkdf2PasswordHasher>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IFlightService, FlightService>();
builder.Services.AddScoped<IBookingService, BookingService>();
builder.Services.AddScoped<IPriceAlertService, PriceAlertService>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();
}

app.UseHttpsRedirection();

app.MapAuthEndpoints();
app.MapFlightEndpoints();
app.MapBookingEndpoints();
app.MapPriceAlertEndpoints();

app.Run();
