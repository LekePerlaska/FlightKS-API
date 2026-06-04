# Rate Limiting — Implementation Plan

A phased plan for a **3-tier rate limiting** setup on the built-in
`Microsoft.AspNetCore.RateLimiting` (no extra package in .NET 10), reusing the
existing `ErrorResponse` contract, configuring `ForwardedHeaders`, and
preparing — but **not** yet implementing — a Redis-backed distributed store for
a true global limit.

## Context (why this shape, for this project)

- **No login/register in this API.** Token issuance is delegated to Keycloak,
  which has its own brute-force detection — so the classic "rate-limit the login
  endpoint" target lives in Keycloak, not here. The real abuse surface in this
  API is (a) anonymous, **DB-heavy search/autocomplete** and (b) money/inventory
  **mutations** (bookings, payments, seat holds).
- **Search hits only Postgres.** `FlightService`/`ItineraryService` search
  execute database queries only — there is no external AI call in the search
  path (the sole `HttpClient` in the services is Keycloak's, and
  `AiSearchDocument` is not queried by search today). So the public-search
  ceiling is governed by **Postgres / Npgsql pool capacity, not an external AI
  quota**. If AI-backed search is added later, revisit this tier.
- **`sub` claim is available post-auth** (`MapInboundClaims=false` in
  `Program.cs`), so authenticated traffic can be partitioned by user identity
  rather than IP.
- **Everything runs behind Docker / a reverse proxy**, so IP partitioning is
  meaningless until `X-Forwarded-For` is honored.
- **The built-in limiter is per-process.** Single container today is fine; if
  the API is ever scaled horizontally, per-instance limits multiply by replica
  count — that is what the future Redis store (Phase 8) solves.

## Existing conventions this plan reuses

- `ErrorResponse` — positional record in `FlightKS.Exceptions`
  (`Type, Title, Status, Code, Detail, Instance, TraceId, Errors?`),
  served as `application/problem+json`.
- Config binding — a class in `Models/Config/` with `const string SectionName`,
  bound via `builder.Services.Configure<T>(GetSection(SectionName))`
  (see `KeycloakOptions`).
- `UseStatusCodePages` switch + `WithStandardErrors` OpenAPI metadata on the
  `/api/v1` group — the 429 code slots into both.

## The three tiers

| Tier | Endpoints | Partition | Algorithm | Why |
|------|-----------|-----------|-----------|-----|
| **Public search** | `/flights/search`, `/itineraries/search`, `/airports/autocomplete` (anonymous) | **IP** | **Token bucket** | Real DoS surface — anonymous, DB-heavy Postgres queries. Token bucket lets a typing burst through, then caps sustained abuse. |
| **Sensitive writes** | `POST /bookings`, `/payments`, refunds, `/seat-reservations` | **User (`sub`)** | **Sliding window**, low permits | Mutate money/inventory. No boundary-burst. Seat holds especially — reservation is **one POST per seat**, so a legit group booking fires up to `maxPassengers` of them back-to-back; rapid spam can lock inventory and starve the seat hub. Protects availability integrity, not just CPU. |
| **Global fallback** | everything else | User if authenticated, else IP | **Sliding window** | Safety net so no route is unprotected. |

---

## Phase 1 — Config & options scaffolding (establishes the Redis seam)

**Goal:** Front-load every tunable number and the future in-memory↔distributed
toggle before any behavior exists.

- Create `FlightApp/Models/Config/RateLimitingOptions.cs` (mirrors
  `KeycloakOptions`: `const string SectionName = "RateLimiting"`), with nested
  option groups per tier (`PublicSearch`, `SensitiveWrites`, `Global`) each
  carrying permit count + window/replenishment, plus a **`Store` enum
  (`InMemory` | `Distributed`)** defaulting to `InMemory`.
- Add a `"RateLimiting"` section to `appsettings.json` and
  `appsettings.Development.json`.
- Bind it in `Program.cs`
  (`builder.Services.Configure<RateLimitingOptions>(...)`) near the existing
  Keycloak binding.

**Suggested starting numbers** (all tunable in config):
- public-search: 20 / 10s, burst capacity 40 (token bucket)
- sensitive-writes: 10 / min (sliding)
- global fallback: 100 / min (sliding)

**Output:** Options bound and injectable. No runtime change yet.
**The `Store` flag is the Redis seam.**

## Phase 2 — ForwardedHeaders

**Goal:** Ensure the real client IP is visible before anything partitions or
logs on it.

- Configure `ForwardedHeadersOptions` (`ForwardedFor | ForwardedProto`) and call
  `app.UseForwardedHeaders()` as the **first** middleware — ahead of
  `UseSerilogRequestLogging()` so both the request log and the limiter see the
  true client IP.
- **Security caveat:** blindly trusting `X-Forwarded-For` lets clients spoof
  their IP and evade IP-based limits. Plan: trust-all in dev (Docker), but
  restrict `KnownProxies`/`KnownNetworks` to the actual proxy range in
  production (driven from config).

**Depends on:** nothing. Foundational, so it lands before the limiter.

## Phase 3 — Rejection response (reuse `ErrorResponse`)

**Goal:** A 429 that's byte-for-byte consistent with the rest of the API's error
contract.

- Build an `OnRejected` callback that emits an `ErrorResponse`
  (`Type=https://httpstatuses.io/429`, `Title="Too Many Requests"`,
  `Status=429`, `Code="rate_limit_exceeded"`, `Detail`, `Instance=request path`,
  `TraceId=Activity.Current?.Id ?? TraceIdentifier`),
  `Content-Type: application/problem+json`, and a `Retry-After` header read from
  the limiter's `RateLimitLease` metadata (`MetadataName.RetryAfter`).
- Add `429` to the existing `UseStatusCodePages` switch as a safety net, and to
  `WithStandardErrors` (the v1-group `ProducesResponseType` metadata) so
  **OpenAPI documents 429** like the other error codes.

**Depends on:** reuses existing `ErrorResponse`.

## Phase 4 — Partition-key + limiter factory (the swappable core)

**Goal:** Isolate the two things Redis will later change — *how a partition key
is derived* and *how a limiter is created* — into one file.

- Create a small strategy (e.g. `Auth/RateLimitPartitioning.cs` or
  `Middleware/`) that centralizes:
  - **(a)** partition key = `sub` claim when `HttpContext.User` is
    authenticated, else `HttpContext.Connection.RemoteIpAddress` (now correct
    thanks to Phase 2);
  - **(b)** limiter construction per tier, branching on
    `RateLimitingOptions.Store`.
- For now: `InMemory` branch builds the built-in `PartitionedRateLimiter` (token
  bucket / sliding window). `Distributed` branch logs a warning and falls back
  to in-memory (so flipping the flag prematurely degrades gracefully rather than
  crashing).

**Output:** One documented seam.
**This is the only file Phase 8 (Redis) touches for behavior.**

## Phase 5 — Register the three policies

**Goal:** Wire the limiter into DI and the pipeline.

- `builder.Services.AddRateLimiter(...)`: a **global limiter** (user-or-IP,
  sliding window — the fallback), a named **`"public-search"`** policy (IP,
  token bucket), a named **`"sensitive-writes"`** policy (user `sub`, sliding
  window). All numbers pulled from `RateLimitingOptions`; all keys/limiters
  built via the Phase 4 strategy. Attach the Phase 3 `OnRejected`; set
  `RejectionStatusCode=429`.
- `app.UseRateLimiter()` placed **after `UseAuthentication()`/
  `UseAuthorization()`** so user-partitioning sees `HttpContext.User`.

**Depends on:** Phases 1, 3, 4.

## Phase 6 — Apply policies to endpoints

**Goal:** Map tiers onto the actual endpoint surface.

- `.RequireRateLimiting("public-search")` on the anonymous read paths —
  `flights/search`, `itineraries/search`, `airports/autocomplete` (decide
  group-level vs. route-level; likely route-level so plain list GETs ride the
  global fallback).
- `.RequireRateLimiting("sensitive-writes")` on `POST /bookings`, `/payments`,
  refunds, `/seat-reservations`.
- Everything else inherits the global fallback. **Exclude the three SignalR
  hubs** (`DisableRateLimiting` / no policy) so long-lived connections aren't
  counted per-request.

**Depends on:** Phase 5.

## Phase 7 — Verify

**Goal:** Prove each tier behaves.

- `docker compose build api` + run. Curl tests:
  - flood autocomplete from one IP → `429` with the `ErrorResponse` body +
    `Retry-After`;
  - confirm an authenticated user gets a separate bucket from anonymous IP;
  - send differing `X-Forwarded-For` values → confirm distinct buckets
    (validates Phase 2);
  - confirm `/openapi/v1.json` lists `429`;
  - confirm the hubs still connect under load.

---

## Phase 8 — Redis distributed store *(planned only — NOT implemented now)*

**Goal (future):** Enforce one true global limit across horizontally-scaled API
replicas, instead of per-instance limits that multiply by replica count.

- Add `StackExchange.Redis`; add a `redis` service to `docker-compose.yml`; add
  its connection string to `RateLimitingOptions`.
- Implement the **`Distributed` branch of the Phase 4 strategy only** — a
  Redis-backed sliding-window limiter (atomic increment + expiry via a Lua
  script for correctness under concurrency).
- Flip `RateLimitingOptions.Store = Distributed` in the scaled environment;
  in-memory stays the default elsewhere.
- **Acceptance:** with 2+ API replicas behind the proxy, a single client's limit
  holds across all of them.
- **Why it's cheap later:** Phases 1 and 4 already created the config flag and
  the single swap point, so no pipeline, policy, rejection, or endpoint code
  changes in Phase 8.

---

## Tuning inputs — how to pick the tier numbers

Every limit needs two anchors: **(A) the legitimate-usage ceiling** (the top of
normal behavior) and **(B) backend safe capacity** (what Postgres / the Npgsql
pool sustains before latency degrades). Rule of thumb: set each limit at a
**safety multiple (~2–3×) of A**, while verifying the *aggregate* across
partitions stays under **B**.

What each parameter maps to:

- **Token bucket** (public-search): `TokenLimit` = max **burst** a legit client
  makes; `TokensPerPeriod`/`ReplenishmentPeriod` = sustained legit **rate**;
  `QueueLimit` = smoothing vs. immediate reject.
- **Sliding window** (writes, global): `PermitLimit` = peak legit **count** in
  `Window`; `SegmentsPerWindow` = granularity.

### Public search (IP, token bucket) — false-positive risk tier

- **Frontend autocomplete behavior**: debounce interval + min-chars-to-fire
  (lives in the `flightks-client` repo). Sets `TokenLimit` (burst) and
  replenishment directly.
- **Max real users behind one IP**: corporate NAT / university / mobile CGNAT —
  the biggest driver of wrongly-blocked legitimate users.
- **Postgres search cost**: p95 query latency, whether search-filtered columns
  are indexed, Npgsql pool size. Aggregate search QPS must stay under what the
  pool sustains.
- **Caching**: search responses are not cached today → every request is a DB
  hit. A future cache would allow higher limits.
- **Bot/scraper tolerance**: public catalog endpoints are prime scrape targets.

### Sensitive writes (user `sub`, sliding) — abuse/cost tier

- **Max legitimate party size: 9 passengers per booking** *(confirmed business
  rule; not yet enforced in code)*. Because seat reservation is **one POST per
  seat**, a full group booking fires up to 9 reservations back-to-back, plus the
  passenger/baggage calls. The sensitive-writes window must clear that burst
  comfortably or it blocks legitimate group bookings — this is the **hard floor**
  for the limit. Worth encoding `maxPassengers = 9` as a shared constant so the
  rate-limit number and the booking validation stay in sync.
- **Payment retry behavior**: how many declined-card retries are legitimate
  before it reads as carding/fraud.
- **Idempotency**: are these endpoints idempotency-keyed? Safe-retry semantics
  change how tight the limit can be.
- **Fraud velocity thresholds** from the business, if any.

### Global fallback (user-or-IP, sliding) — must not break navigation

- **Peak legitimate page fan-out**: the heaviest screen. The booking-confirmation
  flow alone calls `summary` + `price-summary` + `tickets` + `seat-summary` +
  `confirmation`; the SPA initial load fans out further. The fallback must
  comfortably exceed this (2–3×) or normal page loads trip it.

### Cross-cutting

- **Replica count** (see below) — per-instance limits multiply by replicas;
  drives Phase 8 urgency.
- **Anonymous vs. authenticated traffic ratio** — determines whether the IP or
  the user partition dominates.
- **Risk tolerance** — never-block-legit (loose, tune down later) vs. protect
  aggressively (tight, accept some false positives). Sets the safety multiple.
- **Environment** — dev/staging/prod want different numbers; the Phase-1 config
  split already supports this.

### Getting the data cheaply (tooling already in place)

Serilog → Loki → Grafana is running, and `UseSerilogRequestLogging` now emits
per-request lines. So rather than guess:

1. **Mine real traffic** — derive p50/p95/p99 request rates per IP and per user,
   per endpoint group, from existing logs in Grafana. That gives anchor **A**
   empirically.
2. **Roll out observe-only first** — the .NET limiter has no native dry-run, so
   ship generous limits, emit a structured log/metric in `OnRejected` (Phase 3
   centralizes it), watch in Grafana how many *real* users *would* have been
   clipped over ~a week, then ratchet down toward 2–3× p99. Because numbers live
   in config (Phase 1), tightening is a value change + restart, not a code
   redeploy.
3. **Sanity-check against backend** — confirm aggregate allowed QPS stays inside
   the Npgsql pool / Postgres comfort zone.

## Recommended replica count

**Run 1 replica for now.** The codebase has several single-instance assumptions
that must be resolved *before* horizontal scaling is safe — and until they are,
adding replicas causes correctness bugs, not just multiplied rate limits:

1. **Migrate-on-startup race** — `Program.cs:150` calls
   `Database.MigrateAsync()` on boot. Multiple replicas starting together race
   on the same migration. Fix before scaling: run migrations as a separate
   one-shot init step (or gate behind leader election), not per-replica on
   startup.
2. **SignalR has no backplane** — `Program.cs:124` is plain `AddSignalR()`. The
   three hubs (`/hubs/seats`, `/hubs/notifications`, `/hubs/admin-dashboard`)
   only reach clients connected to the *same* instance. With >1 replica, seat
   updates and notifications silently fail to fan out. Fix: a Redis backplane
   (`AddStackExchangeRedis`) — conveniently the **same Redis** introduced in
   Phase 8, so the two efforts share infrastructure.
3. **Uploads on local disk** — `Program.cs:234-238` writes/serves under
   `wwwroot/uploads` via `PhysicalFileProvider`. Files saved on one replica are
   invisible to the others. Fix before scaling: shared/object storage (e.g. S3-
   compatible) or a shared volume.
4. **In-memory rate limiter** — per-process, so N replicas = N× the intended
   global limit. This is exactly what Phase 8 (Redis) resolves.

**Recommendation:** stay at **1 replica** through Phases 1–7; the per-instance
limiter is correct at a single instance. When horizontal scaling becomes a real
requirement, do it as a coordinated step: introduce Redis once and let it serve
**both** the SignalR backplane (#2) and the distributed rate limiter (Phase 8),
and resolve the migration (#1) and uploads (#3) blockers in the same effort.
Target **2 replicas** as the first scaled step (enough for rolling deploys / HA),
then scale on observed load.

## Notes

- Phases 1–7 are the implementable unit now and are independently shippable;
  Phase 8 is deliberately fenced off behind the flag/seam so it can be picked up
  untouched later.
- The suggested numbers in Phase 1 are placeholders — set precisely if target
  traffic figures are known; otherwise they are config-tunable without a
  rebuild.
