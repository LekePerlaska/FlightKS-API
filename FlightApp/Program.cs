using System.Security.Claims;
using System.Text.Json;
using System.Text.Json.Serialization;
using FlightKS.Auth;
using FlightKS.Data;
using FlightKS.Endpoints.V1;
using FlightKS.Endpoints.V1.Admin;
using FlightKS.Endpoints.V1.FlightManager;
using FlightKS.Enums;
using FlightKS.Services;
using FlightKS.Services.Interfaces;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
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

var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? [];
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.WithOrigins(allowedOrigins)
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials();
    });
});

builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.Converters.Add(new JsonStringEnumConverter(JsonNamingPolicy.SnakeCaseLower));
});

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")!;

await using (var bootCtx = new AppDbContext(new DbContextOptionsBuilder<AppDbContext>()
    .UseNpgsql(connectionString)
    .UseSnakeCaseNamingConvention()
    .Options))
{
    await bootCtx.Database.MigrateAsync();
}

var dataSourceBuilder = new NpgsqlDataSourceBuilder(connectionString);
dataSourceBuilder.MapEnum<BookingStatus>("booking_status");
dataSourceBuilder.MapEnum<FlightScheduleStatus>("flight_schedule_status");
dataSourceBuilder.MapEnum<SeatClass>("seat_class");
dataSourceBuilder.MapEnum<FlightSeatStatus>("flight_seat_status");
dataSourceBuilder.MapEnum<TicketStatus>("ticket_status");
dataSourceBuilder.MapEnum<PaymentMethod>("payment_method");
dataSourceBuilder.MapEnum<PaymentStatus>("payment_status");
dataSourceBuilder.MapEnum<RefundStatus>("refund_status");
var dataSource = dataSourceBuilder.Build();

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(dataSource, npg =>
    {
        npg.MapEnum<BookingStatus>("booking_status");
        npg.MapEnum<FlightScheduleStatus>("flight_schedule_status");
        npg.MapEnum<SeatClass>("seat_class");
        npg.MapEnum<FlightSeatStatus>("flight_seat_status");
        npg.MapEnum<TicketStatus>("ticket_status");
        npg.MapEnum<PaymentMethod>("payment_method");
        npg.MapEnum<PaymentStatus>("payment_status");
        npg.MapEnum<RefundStatus>("refund_status");
    }).UseSnakeCaseNamingConvention());

builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ICurrentUserAccessor, CurrentUserAccessor>();
builder.Services.AddTransient<IClaimsTransformation, KeycloakRoleClaimsTransformer>();

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        var keycloak = builder.Configuration.GetSection("Keycloak");
        options.Authority = keycloak["Authority"];
        options.Audience = keycloak["Audience"];
        options.RequireHttpsMetadata = bool.TryParse(keycloak["RequireHttpsMetadata"], out var https) && https;
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = !string.IsNullOrEmpty(keycloak["Audience"]),
            ValidateLifetime = true,
            NameClaimType = "preferred_username",
            RoleClaimType = ClaimTypes.Role,
        };
    });

builder.Services.AddAuthorizationBuilder()
    .AddPolicy(Policies.User, p => p.RequireAuthenticatedUser().RequireRole(Policies.User))
    .AddPolicy(Policies.Admin, p => p.RequireAuthenticatedUser().RequireRole(Policies.Admin))
    .AddPolicy(Policies.FlightManager, p => p.RequireAuthenticatedUser().RequireRole(Policies.FlightManager));

builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<IAirportService, AirportService>();
builder.Services.AddScoped<IAirlineService, AirlineService>();
builder.Services.AddScoped<IAircraftService, AircraftService>();
builder.Services.AddScoped<IFlightService, FlightService>();
builder.Services.AddScoped<IFlightScheduleService, FlightScheduleService>();
builder.Services.AddScoped<IBookingService, BookingService>();
builder.Services.AddScoped<IPassengerService, PassengerService>();
builder.Services.AddScoped<ISeatReservationService, SeatReservationService>();
builder.Services.AddScoped<IBaggageOptionService, BaggageOptionService>();
builder.Services.AddScoped<IBookingBaggageService, BookingBaggageService>();
builder.Services.AddScoped<IPaymentService, PaymentService>();
builder.Services.AddScoped<ITicketService, TicketService>();
builder.Services.AddScoped<INotificationService, NotificationService>();
builder.Services.AddScoped<IDashboardService, DashboardService>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();
}

app.UseHttpsRedirection();
app.UseCors();
app.UseAuthentication();
app.UseAuthorization();

var v1 = app.MapGroup("/api/v1");
v1.MapAuthEndpoints();
v1.MapUsersEndpoints();
v1.MapAirportsEndpoints();
v1.MapAirlinesEndpoints();
v1.MapFlightsEndpoints();
v1.MapFlightSchedulesEndpoints();
v1.MapBaggageOptionsEndpoints();
v1.MapBookingsEndpoints();
v1.MapBookingPassengersEndpoints();
v1.MapSeatReservationsEndpoints();
v1.MapBookingBaggageEndpoints();
v1.MapPaymentsEndpoints();
v1.MapNotificationsEndpoints();
v1.MapAdminDashboardEndpoints();
v1.MapAdminAirportsEndpoints();
v1.MapAdminAirlinesEndpoints();
v1.MapAdminAircraftsEndpoints();
v1.MapAdminFlightsEndpoints();
v1.MapAdminFlightSchedulesEndpoints();
v1.MapFlightManagerDashboardEndpoints();
v1.MapFlightManagerSchedulesEndpoints();

app.Run();
