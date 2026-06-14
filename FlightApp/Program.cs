using System.Diagnostics;
using System.Text.Json;
using System.Text.Json.Serialization;
using FlightKS.Auth;
using FlightKS.Exceptions;
using FlightKS.Data;
using FlightKS.Endpoints;
using FlightKS.Endpoints.V1;
using FlightKS.Endpoints.V1.Admin;
using FlightKS.Endpoints.V1.FlightManager;
using FluentValidation;
using FlightKS.Enums;
using FlightKS.Hubs;
using FlightKS.Middleware;
using FlightKS.Models.Config;
using FlightKS.Services;
using FlightKS.Services.Interfaces;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.RateLimiting;
using StackExchange.Redis;
using System.Threading.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using Prometheus;
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
builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();

// Keycloak
builder.Services.Configure<KeycloakOptions>(
    builder.Configuration.GetSection(KeycloakOptions.SectionName));

// Rate limiting
builder.Services.Configure<RateLimitingOptions>(
    builder.Configuration.GetSection(RateLimitingOptions.SectionName));

var rateLimitOptions = builder.Configuration
    .GetSection(RateLimitingOptions.SectionName)
    .Get<RateLimitingOptions>() ?? new RateLimitingOptions();

if (rateLimitOptions.Store == RateLimitStore.Distributed)
{
    Log.Information("RateLimiting: Distributed mode — connecting to Redis at {ConnectionString}",
        rateLimitOptions.RedisConnectionString);
    builder.Services.AddSingleton<IConnectionMultiplexer>(
        _ => ConnectionMultiplexer.Connect(rateLimitOptions.RedisConnectionString));
}
else
{
    Log.Information("RateLimiting: InMemory mode (per-instance). " +
                    "Set Store=Distributed and start Redis to enforce global limits across replicas.");
}

// Resolves IConnectionMultiplexer per request-context when Distributed; null otherwise.
// IConnectionMultiplexer is a singleton so resolution is a cheap DI cache lookup.
IConnectionMultiplexer? GetMux(HttpContext ctx) =>
    rateLimitOptions.Store == RateLimitStore.Distributed
        ? ctx.RequestServices.GetService<IConnectionMultiplexer>()
        : null;

builder.Services.AddRateLimiter(limiterOptions =>
{
    limiterOptions.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    limiterOptions.OnRejected = RateLimitRejectionHandler.OnRejected;

    // Global fallback — applies to every route that does not have a named policy.
    // Partitioned by user (sub) when authenticated, IP when anonymous.
    limiterOptions.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(ctx =>
        RateLimitPartitioning.GetGlobalPartition(
            RateLimitPartitioning.GetPartitionKey(ctx),
            rateLimitOptions.Global,
            rateLimitOptions.Store,
            GetMux(ctx)));

    // Anonymous search/autocomplete — token bucket (in-memory) or sliding window (distributed).
    limiterOptions.AddPolicy<string>(RateLimitPartitioning.PublicSearchPolicy, ctx =>
        RateLimitPartitioning.GetPublicSearchPartition(
            RateLimitPartitioning.GetPartitionKey(ctx),
            rateLimitOptions.PublicSearch,
            rateLimitOptions.Store,
            GetMux(ctx)));

    // Money/inventory mutations — sliding window per user (sub).
    limiterOptions.AddPolicy<string>(RateLimitPartitioning.SensitiveWritesPolicy, ctx =>
        RateLimitPartitioning.GetSensitiveWritesPartition(
            RateLimitPartitioning.GetPartitionKey(ctx),
            rateLimitOptions.SensitiveWrites,
            rateLimitOptions.Store,
            GetMux(ctx)));
});

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        // Public issuer the tokens carry (the edge origin, e.g. http://localhost/realms/flightks).
        // The token's `iss` is validated against the issuer discovered from the metadata below.
        var authority = builder.Configuration["Keycloak:Authority"];
        if (!string.IsNullOrEmpty(authority))
            options.Authority = authority;

        // Where the API fetches OIDC metadata + JWKS. In containers this is the INTERNAL
        // Keycloak address (e.g. http://keycloak:8080/realms/flightks/.well-known/openid-configuration).
        // Keycloak (KC_HOSTNAME + hostname-backchannel-dynamic) keeps the issuer at the public edge
        // URL while serving reachable backchannel URLs — so no host rewriting is needed.
        var metadataAddress = builder.Configuration["Keycloak:MetadataAddress"];
        if (!string.IsNullOrEmpty(metadataAddress))
            options.MetadataAddress = metadataAddress;

        options.RequireHttpsMetadata = builder.Configuration.GetValue<bool>("Keycloak:RequireHttpsMetadata");
        options.MapInboundClaims = false;

        var audience = builder.Configuration["Keycloak:Audience"];
        options.TokenValidationParameters = new()
        {
            ValidateAudience = !string.IsNullOrEmpty(audience),
            ValidAudiences = string.IsNullOrEmpty(audience) ? [] : [audience],
            ValidateIssuer = true,
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

            OnChallenge = async context =>
            {
                context.HandleResponse();
                var error = new ErrorResponse(
                    Type: "https://httpstatuses.io/401",
                    Title: "Unauthorized",
                    Status: 401,
                    Code: "unauthorized",
                    Detail: "Authentication is required. Provide a valid Bearer token.",
                    Instance: context.HttpContext.Request.Path,
                    TraceId: Activity.Current?.Id ?? context.HttpContext.TraceIdentifier);
                context.HttpContext.Response.StatusCode = 401;
                context.HttpContext.Response.ContentType = "application/problem+json";
                await context.HttpContext.Response.WriteAsJsonAsync(error, context.HttpContext.RequestAborted);
            },

            OnForbidden = async context =>
            {
                var error = new ErrorResponse(
                    Type: "https://httpstatuses.io/403",
                    Title: "Forbidden",
                    Status: 403,
                    Code: "forbidden",
                    Detail: "You do not have permission to access this resource.",
                    Instance: context.HttpContext.Request.Path,
                    TraceId: Activity.Current?.Id ?? context.HttpContext.TraceIdentifier);
                context.HttpContext.Response.StatusCode = 403;
                context.HttpContext.Response.ContentType = "application/problem+json";
                await context.HttpContext.Response.WriteAsJsonAsync(error, context.HttpContext.RequestAborted);
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

var dpKeysPath = builder.Configuration["DataProtection:KeysPath"] ?? "/app/dp-keys";
builder.Services.AddDataProtection()
    .PersistKeysToFileSystem(new DirectoryInfo(dpKeysPath));

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
builder.Services.AddScoped<IFlightManagerService, FlightManagerService>();

builder.Services.AddValidatorsFromAssemblyContaining<Program>(ServiceLifetime.Scoped);

var hcBuilder = builder.Services.AddHealthChecks()
    .AddDbContextCheck<AppDbContext>("postgres");

if (rateLimitOptions.Store == RateLimitStore.Distributed)
    hcBuilder.Add(new Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckRegistration(
        "redis",
        sp => new FlightKS.HealthChecks.RedisHealthCheck(sp.GetRequiredService<IConnectionMultiplexer>()),
        Microsoft.Extensions.Diagnostics.HealthChecks.HealthStatus.Unhealthy,
        ["redis"]));

var app = builder.Build();

// ForwardedHeaders — must be first so the real client IP is visible to both
// UseSerilogRequestLogging and the rate limiter.
// Dev: trust all sources (no reverse proxy in the compose stack).
// Prod: set ForwardedHeaders:KnownProxies (individual IPs) and/or
//       ForwardedHeaders:KnownNetworks (CIDR ranges, e.g. 172.16.0.0/12 for
//       the Docker bridge network when the reverse proxy runs as a sidecar container).
var fwdOptions = new ForwardedHeadersOptions
{
    ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto,
};
if (app.Environment.IsDevelopment())
{
    fwdOptions.KnownIPNetworks.Clear();
    fwdOptions.KnownProxies.Clear();
}
else
{
    var knownProxies = builder.Configuration
        .GetSection("ForwardedHeaders:KnownProxies")
        .Get<string[]>() ?? [];
    foreach (var proxy in knownProxies)
    {
        if (System.Net.IPAddress.TryParse(proxy, out var ip))
            fwdOptions.KnownProxies.Add(ip);
    }

    var knownNetworks = builder.Configuration
        .GetSection("ForwardedHeaders:KnownNetworks")
        .Get<string[]>() ?? [];
    foreach (var network in knownNetworks)
    {
        var parts = network.Split('/');
        if (parts.Length == 2 &&
            System.Net.IPAddress.TryParse(parts[0], out var prefix) &&
            int.TryParse(parts[1], out var prefixLength))
        {
            fwdOptions.KnownIPNetworks.Add(new System.Net.IPNetwork(prefix, prefixLength));
        }
    }
}
app.UseForwardedHeaders(fwdOptions);
app.UseHttpMetrics();

if (!app.Environment.IsDevelopment())
{
    app.UseHsts();
}

app.UseSerilogRequestLogging();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();
}

app.UseExceptionHandler();

app.UseStatusCodePages(async ctx =>
{
    var httpContext = ctx.HttpContext;
    var status = httpContext.Response.StatusCode;
    var (title, code, detail) = status switch
    {
        401 => ("Unauthorized", "unauthorized", "Authentication is required."),
        403 => ("Forbidden", "forbidden", "You do not have permission to access this resource."),
        404 => ("Not Found", "not_found", "The requested resource was not found."),
        405 => ("Method Not Allowed", "method_not_allowed", "The HTTP method is not allowed for this endpoint."),
        429 => ("Too Many Requests", "rate_limit_exceeded", "You have exceeded the request rate limit. Please slow down and try again."),
        _ => ("Error", "error", "An error occurred."),
    };
    var error = new ErrorResponse(
        Type: $"https://httpstatuses.io/{status}",
        Title: title,
        Status: status,
        Code: code,
        Detail: detail,
        Instance: httpContext.Request.Path,
        TraceId: Activity.Current?.Id ?? httpContext.TraceIdentifier);
    httpContext.Response.ContentType = "application/problem+json";
    await httpContext.Response.WriteAsJsonAsync(error, httpContext.RequestAborted);
});

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
app.UseRateLimiter();

var v1 = app.MapGroup("/api/v1").WithStandardErrors();
v1.MapAuthEndpoints();
v1.MapUsersEndpoints();
v1.MapAirportsEndpoints();
v1.MapTimezonesEndpoints();
v1.MapAirlinesEndpoints();
v1.MapFlightsEndpoints();
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
v1.MapAdminBaggageOptionsEndpoints();
v1.MapPaymentRefundsEndpoints();
v1.MapFlightManagerDashboardEndpoints();
v1.MapFlightManagerSchedulesEndpoints();
v1.MapFlightManagerTicketsEndpoints();

app.MapHub<SeatHub>("/hubs/seats").DisableRateLimiting();
app.MapHub<NotificationHub>("/hubs/notifications").DisableRateLimiting();
app.MapHub<AdminDashboardHub>("/hubs/admin-dashboard").DisableRateLimiting();

app.MapHealthChecks("/health", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions
{
    ResponseWriter = async (ctx, report) =>
    {
        ctx.Response.ContentType = "application/json";
        await ctx.Response.WriteAsJsonAsync(new
        {
            status = report.Status.ToString(),
            checks = report.Entries.Select(e => new
            {
                name = e.Key,
                status = e.Value.Status.ToString(),
                description = e.Value.Description,
            }),
        });
    },
});

app.MapMetrics("/metrics").DisableRateLimiting();

app.Run();

// Exposes Program to WebApplicationFactory<Program> in integration tests.
public partial class Program { }
