# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Commands

The host SDK is .NET 8.0 but this project targets **.NET 10.0**, so builds and runs go through Docker, not the host `dotnet` CLI.

```bash
docker compose build api      # Build the API image (SDK 10 in-container)
docker compose up -d          # Start the full stack (api, postgres, keycloak, loki, grafana, frontend)
docker compose up -d api      # Start just the API (+ its dependencies)
docker compose down           # Stop and remove containers (named volumes are preserved)
docker compose logs -f api    # Tail API logs
```

Requires a `.env` file (see `.env.example`) for Postgres/Keycloak credentials.

Service URLs once up:
- API: `http://localhost:5194` (in-container port 5194)
- API docs (Scalar): `http://localhost:5194/scalar/v1`; OpenAPI JSON at `/openapi/v1.json`
- Keycloak: `http://localhost:8080` (realm `flightks`)
- Grafana: `http://localhost:3001` (Loki datasource provisioned)
- Frontend (Next.js): `http://localhost:3000` (mounted from `../flightks-client/flightks-client`)
- Postgres: host port `5433`

The app applies pending migrations on startup via `MigrateAsync` in `Program.cs`.

### Generating migrations

The final API image is a runtime image (`mcr.microsoft.com/dotnet/aspnet:10.0`) — it does **not** contain the EF tooling. Use a temporary SDK container instead:

```bash
docker run --rm \
  -v $(pwd):/src \
  -w /src/FlightApp \
  mcr.microsoft.com/dotnet/sdk:10.0 \
  sh -c "dotnet tool install --global dotnet-ef 2>/dev/null; export PATH=\"\$PATH:/root/.dotnet/tools\" && dotnet restore && dotnet ef migrations add <MigrationName>"
```

### Running tests

Three test projects live under `tests/`. Because the host SDK is 8.0, tests also run inside an SDK 10 container:

```bash
# Pure unit tests (validators, mappers — no Docker socket needed)
docker run --rm -v $(pwd):/src -w /src mcr.microsoft.com/dotnet/sdk:10.0 dotnet test tests/FlightKS.UnitTests

# Service tests (Testcontainers spins a real Postgres)
# --network=host lets the SDK container reach the mapped Postgres port
# TESTCONTAINERS_RYUK_DISABLED=true skips the Ryuk reaper (can't start privileged containers inside Docker)
docker run --rm --network=host -v $(pwd):/src -v /var/run/docker.sock:/var/run/docker.sock -e TESTCONTAINERS_RYUK_DISABLED=true -w /src mcr.microsoft.com/dotnet/sdk:10.0 dotnet test tests/FlightKS.ServiceTests

# Integration tests (full ASP.NET Core host + Testcontainers Postgres + test auth scheme)
docker run --rm --network=host -v $(pwd):/src -v /var/run/docker.sock:/var/run/docker.sock -e TESTCONTAINERS_RYUK_DISABLED=true -w /src mcr.microsoft.com/dotnet/sdk:10.0 dotnet test tests/FlightKS.IntegrationTests

# All tests at once
docker run --rm --network=host -v $(pwd):/src -v /var/run/docker.sock:/var/run/docker.sock -e TESTCONTAINERS_RYUK_DISABLED=true -w /src mcr.microsoft.com/dotnet/sdk:10.0 dotnet test FlightKS.sln
```

### Test project breakdown

| Project | What it tests | Dependencies |
|---|---|---|
| `FlightKS.UnitTests` | Mappers, FluentValidation validators, exception types | None — fully isolated |
| `FlightKS.ServiceTests` | Service business logic against a real Postgres (Testcontainers + Respawn) | Docker socket, NSubstitute for SignalR hubs and notification service |
| `FlightKS.IntegrationTests` | Full HTTP endpoint behaviour via `WebApplicationFactory` — auth, authorization, request/response shapes | Docker socket, stub Keycloak service, test auth handler |

**Service test pattern.** Many services depend on `INotificationService`, `IHubContext<T>`, `IEmailSender`, or `ILogger`. Stub them with NSubstitute — both `CreateAsync` and `CreateBulkAsync` need to be stubbed:

```csharp
private static INotificationService MakeNotifications()
{
    var n = Substitute.For<INotificationService>();
    n.CreateAsync(default, default!, default!, default!)
     .ReturnsForAnyArgs(Task.FromResult(new Notification { Title = "", Message = "", Type = "" }));
    n.CreateBulkAsync(default!, default!, default!, default!)
     .ReturnsForAnyArgs(Task.CompletedTask);
    return n;
}
private static MyService MakeSut(AppDbContext db) => new(db, MakeNotifications());
```

`FlightApp/FlightKS.http` contains sample API requests for the VS Code REST Client extension.

## Architecture

An **ASP.NET Core 10.0 Web API** (minimal hosting model, no `Startup.cs`) for a flight-booking platform. The entire bootstrap lives in `FlightApp/Program.cs`. All HTTP endpoints are versioned under **`/api/v1`**.

- **Framework:** .NET 10.0, C# 14, nullable reference types enabled
- **Database:** PostgreSQL via EF Core 10 + Npgsql. snake_case naming (`EFCore.NamingConventions`), 8 native PG enums mapped on both the data source and the EF `UseNpgsql` callback, soft-delete query filters (`DeletedAt`), and `gen_random_uuid()` Guid PKs. Schema config is per-entity in `FlightApp/Data/DBContext.cs`; `AppDbContextDesignTimeFactory` lets EF tooling build the context without running startup.
- **Auth:** Keycloak JWT bearer (realm `flightks`). `KeycloakRoleClaimsTransformer` maps `realm_access.roles` → role claims; `ICurrentUserAccessor`/`CurrentUserAccessor` resolves the Keycloak `sub` to a local `User` (via `KeycloakUserId`). Three role policies in `FlightApp/Auth/Policies.cs`: **User**, **Admin**, **FlightManager**. `KeycloakBackchannelHandler` rewrites the JWKS URI inside Docker.
- **Real-time:** Three SignalR hubs — `/hubs/seats`, `/hubs/notifications`, `/hubs/admin-dashboard`. Hub auth reads the token from an `access_token` query-string param. `NotificationHub.OnConnectedAsync` joins each connection to a `user:{localId}` group (fails gracefully if DB is unavailable — connection accepted but not grouped). Redis backplane configured via `SignalR:RedisConnectionString` (must include password: `redis:6379,password=${REDIS_PASSWORD},abortConnect=false`).
- **Notifications:** `NotificationService.CreateAsync` is the single write entry point — persists the DB row, pushes `{ notification, unreadCount }` to the user's SignalR group, and optionally fires email. For broadcast events (flight cancellation, delays), use `CreateBulkAsync` instead — it batches the INSERT, fetches all unread counts in one GROUP BY, and pushes hub events via `Task.WhenAll`. Never call `CreateAsync` in a loop over passengers.
- **Email:** MailKit SMTP via `IEmailSender` / `EmailSender`. Registers as `NullEmailSender` (no-op) when `Email:Host` is empty. Templates are static HTML methods in `FlightApp/Services/EmailTemplates.cs`. Email fires fire-and-forget after the hub push (`ContinueWith(OnlyOnFaulted, CancellationToken.None, TaskScheduler.Default)`) so SMTP latency never blocks a request. Set `EMAIL_HOST` in `.env` to enable; use Mailpit (`axllent/mailpit`, ports 1025/8025) for local testing.
- **API docs:** Scalar + native OpenAPI (`AddOpenApi`/`MapScalarApiReference`), dev-only.
- **Observability:** Serilog → console + Grafana Loki sink.
- **Files:** uploads are written under `wwwroot/uploads` and served as static files.
- **Solution:** `FlightKS.sln` at the repo root; the single project is `FlightApp/`.

### Notification coverage

| Event | SignalR | Email |
|---|---|---|
| Booking confirmed (payment) | ✅ | ✅ |
| Refund processed | ✅ | ✅ |
| Admin cancels booking | ✅ | ✅ |
| Flight cancelled | ✅ `CreateBulkAsync` | ✅ |
| Flight delayed | ✅ `CreateBulkAsync` | ✅ |
| Departure time changed | ✅ `CreateBulkAsync` | ✅ |
| Gate change, check-in, ticket cancelled, admin booking status | ✅ | — |
| FlightManager manual broadcast | ✅ `CreateBulkAsync` | ✅ |

### FlightManager scoping

Every `FlightManager` user has a nullable `User.AirlineId` FK. Until it is set, the manager sees no schedules and all per-schedule endpoints return 404.

**Assign an airline (admin only):**
```
PATCH /api/v1/admin/users/{id}/airline
{ "airlineId": "<guid>" }   // pass null to clear
```

All FlightManager per-schedule endpoints (`Patch`, `Passengers`, `Seats`, `SetSeatStatus`, `Notify`, `ExportManifest`) call `BelongsToFlightManagerAsync` before acting and return 404 for out-of-scope schedules.

### Key business rules

- **Check-in:** `CheckInTicketAsync` requires `booking.Status == Confirmed`. A passenger on an unpaid booking cannot board.
- **Ticket status:** `Cancelled` and `Refunded` are terminal — `UpdateStatusAsync` throws `BusinessRuleException` if the current status is either. Cannot be reversed.
- **Aircraft overlap:** `EnsureAircraftIsFreeAsync` uses `IgnoreQueryFilters()` so soft-deleted schedules still block their time slot.
- **Admin flight schedule routes:** `PATCH /{id}` (status/gate/delay only, uses `FlightScheduleStatusUpdateDto`); `PUT /{id}` (full update including price, seats, cabin pricing, uses `FlightScheduleUpdateDto`).

### Layout

- `FlightApp/Endpoints/V1/` — public + authenticated-user minimal-API endpoint groups (auth, users, airports, airlines, flights, itineraries, baggage, bookings, passengers, seat reservations, payments, refunds, tickets, notifications, timezones). Subfolders `Admin/` and `FlightManager/` hold the role-gated endpoint groups. Files are named after their would-be MVC controllers. Each group is a `MapXxxEndpoints` extension wired in `Program.cs` onto the `/api/v1` group.
- `FlightApp/Services/` + `Services/Interfaces/` — business logic, one service per domain, all registered scoped in `Program.cs`.
- `FlightApp/Models/Entities/` — EF entities (~25). `Models/Dtos/` — request/response records grouped by domain. `Models/Config/` (includes `EmailOptions`), `Models/Pricing/`.
- `FlightApp/Mappers/` — entity → DTO extension methods.
- `FlightApp/Enums/` — one enum per file (`FlightKS.Enums`); PG-backed enums are registered in both `Program.cs` (`MapEnum`) and `DBContext.cs` (`HasPostgresEnum`).
- `FlightApp/Migrations/`, `FlightApp/Hubs/`, `FlightApp/Auth/`.

### Domain model

Catalog (Airline, Airport with timezone, Aircraft, Seat) → Scheduling (Flight route, FlightSchedule, per-cabin FlightSchedulePrice, FlightSeat, multi-leg Itinerary + ItinerarySegment) → Booking flow (Booking → Passenger → Ticket → Payment → PaymentRefund, plus BaggageOption / BookingBaggage). Platform entities: Notification, UploadedFile, FeatureFlag, AdminLog, AuditLog (jsonb), AiSearchDocument. `User.AirlineId` scopes FlightManager users to a single airline.

When adding a new feature, the typical path is: entity (+ `DBContext.cs` config) → migration → DTOs → mapper → service (+ interface, registered in `Program.cs`) → endpoint group (+ `MapXxxEndpoints` in `Program.cs`, with `RequireAuthorization(Policies.*)` if protected).
