using System.Text.Json;
using System.Text.Json.Serialization;
using FlightKS.Auth;
using FlightKS.Data;
using FlightKS.Endpoints.V1;
using FlightKS.Endpoints.V1.Admin;
using FlightKS.Endpoints.V1.FlightManager;
using FlightKS.Enums;
using FlightKS.Hubs;
using FlightKS.Models.Config;
using FlightKS.Services;
using FlightKS.Services.Interfaces;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
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

// Keycloak
builder.Services.Configure<KeycloakOptions>(
    builder.Configuration.GetSection(KeycloakOptions.SectionName));

var keycloakAuthority = builder.Configuration["Keycloak:Authority"] ?? string.Empty;
var runningInDocker = keycloakAuthority.Contains("keycloak:", StringComparison.OrdinalIgnoreCase);

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.Authority = keycloakAuthority;
        options.RequireHttpsMetadata = builder.Configuration.GetValue<bool>("Keycloak:RequireHttpsMetadata");
        options.MapInboundClaims = false;
        if (runningInDocker)
        {
            // Keycloak's discovery doc returns localhost:8080 for jwks_uri, but that
            // doesn't resolve inside Docker — rewrite to the internal service name.
            options.BackchannelHttpHandler = new KeycloakBackchannelHandler(new HttpClientHandler());
        }
        options.TokenValidationParameters = new()
        {
            ValidateAudience = false,
            // Token iss is localhost:8080 but authority inside Docker is keycloak:8080
            ValidateIssuer = false,
            NameClaimType = "preferred_username",
        };
        options.Events = new JwtBearerEvents
        {
            OnMessageReceived = context =>
            {
                var accessToken = context.Request.Query["access_token"];
                if (!string.IsNullOrEmpty(accessToken) &&
                    context.Request.Path.StartsWithSegments("/hubs"))
                {
                    context.Token = accessToken;
                }
                return Task.CompletedTask;
            },
        };
    });

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy(Policies.User, policy => policy.RequireRole("User"));
    options.AddPolicy(Policies.Admin, policy => policy.RequireRole("Admin"));
    options.AddPolicy(Policies.FlightManager, policy => policy.RequireRole("FlightManager"));
});
builder.Services.AddTransient<IClaimsTransformation, KeycloakRoleClaimsTransformer>();
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ICurrentUserAccessor, CurrentUserAccessor>();
builder.Services.AddMemoryCache();
builder.Services.AddHttpClient<IKeycloakService, KeycloakService>();
builder.Services.AddSignalR();

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

builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<IAirportService, AirportService>();
builder.Services.AddScoped<IAirlineService, AirlineService>();
builder.Services.AddScoped<IAircraftService, AircraftService>();
builder.Services.AddScoped<IFlightService, FlightService>();
builder.Services.AddScoped<IFlightScheduleService, FlightScheduleService>();
builder.Services.AddScoped<IItineraryService, ItineraryService>();
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

// Ensure the uploads directory exists and is served as static files
var uploadsRoot = Path.Combine(app.Environment.ContentRootPath, "wwwroot");
Directory.CreateDirectory(Path.Combine(uploadsRoot, "uploads"));
app.UseStaticFiles(new StaticFileOptions
{
    FileProvider = new Microsoft.Extensions.FileProviders.PhysicalFileProvider(uploadsRoot),
    RequestPath = "",
});

app.UseCors();
app.UseAuthentication();
app.UseAuthorization();

var v1 = app.MapGroup("/api/v1");
v1.MapAuthEndpoints();
v1.MapUsersEndpoints();
v1.MapAirportsEndpoints();
v1.MapTimezonesEndpoints();
v1.MapAirlinesEndpoints();
v1.MapFlightsEndpoints();
v1.MapFlightSchedulesEndpoints();
v1.MapItinerariesEndpoints();
v1.MapBaggageOptionsEndpoints();
v1.MapBookingsEndpoints();
v1.MapBookingPassengersEndpoints();
v1.MapSeatReservationsEndpoints();
v1.MapBookingBaggageEndpoints();
v1.MapPaymentsEndpoints();
v1.MapTicketsEndpoints();
v1.MapNotificationsEndpoints();
v1.MapAdminDashboardEndpoints();
v1.MapAdminUsersEndpoints();
v1.MapAdminAirportsEndpoints();
v1.MapAdminAirlinesEndpoints();
v1.MapAdminAircraftsEndpoints();
v1.MapAdminFlightsEndpoints();
v1.MapAdminFlightSchedulesEndpoints();
v1.MapAdminItinerariesEndpoints();
v1.MapAdminBookingsEndpoints();
v1.MapPaymentRefundsEndpoints();
v1.MapFlightManagerDashboardEndpoints();
v1.MapFlightManagerSchedulesEndpoints();

app.MapHub<SeatHub>("/hubs/seats");
app.MapHub<NotificationHub>("/hubs/notifications");
app.MapHub<AdminDashboardHub>("/hubs/admin-dashboard");

app.Run();
