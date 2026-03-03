# Product Requirements Document — WeatherDashboard

> **Version:** 1.0 (regenerated from implemented codebase)
> **Status:** Fully Implemented
> **Last Updated:** 2025-07-16

---

## Table of Contents

1. [Overview](#1-overview)
2. [Architecture](#2-architecture)
3. [Services](#3-services)
4. [Aspire Integrations](#4-aspire-integrations)
5. [Data Models](#5-data-models)
6. [API Contracts](#6-api-contracts)
7. [Frontend](#7-frontend)
8. [Background Worker](#8-background-worker)
9. [Observability](#9-observability)
10. [Testing](#10-testing)
11. [Work Items](#11-work-items)

---

## 1. Overview

### Project Name

**WeatherDashboard** — A real-time weather dashboard built with .NET Aspire.

### Description

WeatherDashboard is a cloud-native, multi-service application that lets users search for current weather conditions, view 5-day forecasts, and manage a list of favorite cities. The system is composed of a Blazor Server frontend, two REST APIs (weather data and user preferences), and a background worker — all orchestrated by .NET Aspire with Redis caching, PostgreSQL persistence, and full OpenTelemetry observability.

### Goals

| Goal | Description |
|------|-------------|
| **Demonstrate .NET Aspire** | Showcase Aspire orchestration, service discovery, health checks, and OpenTelemetry in a realistic multi-service app |
| **Real-time weather data** | Provide current conditions and 5-day forecasts from OpenWeatherMap, cached in Redis for performance |
| **User preferences** | Allow users to save, view, and remove favorite cities, persisted in PostgreSQL via EF Core |
| **Background refresh** | Automatically refresh cached weather data for all saved cities on a 15-minute cycle |
| **Observability** | Full distributed tracing, metrics, structured logging, and health checks across all services |
| **Testability** | Integration tests that boot the full Aspire app and validate real service-to-service communication |

### Technology Stack

| Layer | Technology |
|-------|-----------|
| Orchestration | .NET Aspire (AppHost) |
| Frontend | Blazor Server (Interactive SSR) |
| APIs | ASP.NET Core Minimal APIs |
| Caching | Redis (Aspire-managed container) |
| Database | PostgreSQL (Aspire-managed container) |
| ORM | Entity Framework Core (Npgsql) |
| External Data | OpenWeatherMap API (free tier) |
| Worker | .NET Generic Host + BackgroundService |
| Observability | OpenTelemetry (OTLP), Aspire Dashboard |
| Testing | xUnit + Aspire.Hosting.Testing |
| Runtime | .NET 10 |

---

## 2. Architecture

### Service Diagram

```
┌─────────────────────────────────────────────────────────────┐
│                     Aspire AppHost                          │
│              (Orchestration & Service Discovery)            │
├─────────────────────────────────────────────────────────────┤
│                                                             │
│  ┌───────────┐     ┌──────────────┐     ┌───────────────┐  │
│  │ Frontend   │────▶│ Weather API  │────▶│    Redis      │  │
│  │ (Blazor)   │     │ (ASP.NET)    │     │   (Cache)     │  │
│  │            │     └──────────────┘     └───────────────┘  │
│  │            │                                             │
│  │            │     ┌──────────────┐     ┌───────────────┐  │
│  │            │────▶│ Preferences  │────▶│ PostgreSQL    │  │
│  └───────────┘     │ API (ASP.NET)│     │   (Data)      │  │
│                     └──────────────┘     └───────────────┘  │
│  ┌───────────┐           ▲                                  │
│  │  Worker    │───────────┘                                 │
│  │ (Background│──────▶ Weather API                          │
│  │  Service)  │                                             │
│  └───────────┘                                              │
│                                                             │
│  ┌─────────────────────────────────────────────────────┐    │
│  │              ServiceDefaults                        │    │
│  │  (OpenTelemetry, Health Checks, Resilience)         │    │
│  └─────────────────────────────────────────────────────┘    │
└─────────────────────────────────────────────────────────────┘
```

### Dependency Graph (WithReference / WaitFor Chains)

The AppHost (`AppHost.cs`) defines the following resource graph:

```
redis (container)
  └──▶ weatherapi (WithReference + WaitFor)

postgres → preferencesdb (container + database)
  └──▶ preferencesapi (WithReference + WaitFor)

weatherapi ──▶ frontend    (WithReference + WaitFor)
preferencesapi ──▶ frontend (WithReference + WaitFor)

weatherapi ──▶ worker      (WithReference + WaitFor)
preferencesapi ──▶ worker   (WithReference + WaitFor)
```

**Startup order enforced by WaitFor:**
1. Redis and PostgreSQL containers start first
2. Weather API waits for Redis; Preferences API waits for PostgreSQL
3. Frontend and Worker wait for both APIs to be healthy

### Project Structure

```
dotnet-aspire-demo/
├── WeatherDashboard.AppHost/              # Aspire orchestrator
│   └── AppHost.cs                         # Resource definitions & dependency wiring
├── WeatherDashboard.ServiceDefaults/      # Shared Aspire defaults
│   └── Extensions.cs                      # OpenTelemetry, health checks, resilience, service discovery
├── WeatherDashboard.WeatherApi/           # Weather REST API
│   ├── Program.cs                         # Host setup, Redis cache, OpenWeatherMap HttpClient
│   ├── Endpoints/WeatherEndpoints.cs      # GET /api/weather/{city}, GET /api/weather/{city}/forecast
│   ├── Services/OpenWeatherMapService.cs  # External API client (data parsing, F/C conversion)
│   ├── Services/WeatherCacheService.cs    # Redis cache-aside with stale fallback
│   ├── Models/CurrentWeather.cs           # Current weather record
│   ├── Models/Forecast.cs                 # Forecast + ForecastDay records
│   └── HealthChecks/OpenWeatherMapHealthCheck.cs  # External API reachability check
├── WeatherDashboard.PreferencesApi/       # Preferences REST API
│   ├── Program.cs                         # Host setup, Npgsql/EF Core, auto-migration
│   ├── Endpoints/PreferencesEndpoints.cs  # CRUD endpoints for city preferences
│   ├── Models/UserPreference.cs           # EF Core entity
│   └── Data/PreferencesDbContext.cs       # DbContext with seed data and indexes
├── WeatherDashboard.Frontend/             # Blazor Server UI
│   ├── Program.cs                         # Host setup, HttpClient registration via service discovery
│   ├── Components/
│   │   ├── App.razor                      # Root HTML shell
│   │   ├── Routes.razor                   # Router
│   │   ├── Layout/MainLayout.razor        # Layout wrapper
│   │   └── Pages/
│   │       ├── Home.razor                 # Dashboard with weather cards, add/remove cities
│   │       ├── Forecast.razor             # 5-day forecast detail page
│   │       ├── Error.razor                # Error page
│   │       └── NotFound.razor             # 404 page
│   ├── Services/
│   │   ├── WeatherApiClient.cs            # Typed HttpClient for Weather API
│   │   └── PreferencesApiClient.cs        # Typed HttpClient for Preferences API
│   └── Models/                            # Client-side DTOs (mirrors API models)
│       ├── CurrentWeather.cs
│       ├── Forecast.cs
│       └── UserPreference.cs
├── WeatherDashboard.Worker/               # Background refresh service
│   ├── Program.cs                         # Host setup, HttpClient registration, health check
│   └── Services/
│       ├── WeatherRefreshWorker.cs        # BackgroundService with retry + stagger
│       └── WeatherRefreshHealthCheck.cs   # Health check tracking refresh cycle status
├── WeatherDashboard.Tests/                # Integration tests
│   ├── AspireAppFixture.cs                # Shared fixture that boots full Aspire app
│   ├── WeatherApiTests.cs                 # Weather API endpoint + caching tests
│   ├── PreferencesApiTests.cs             # Preferences CRUD tests
│   ├── ServiceDiscoveryTests.cs           # E2E service discovery validation
│   └── ResilienceTests.cs                 # Resilience + graceful degradation tests
└── docs/
    ├── PRD.md                             # This document
    └── telemetry-verification.md          # OpenTelemetry trace verification guide
```

---

## 3. Services

### 3.1 AppHost (Orchestrator)

| Attribute | Value |
|-----------|-------|
| **Project** | `WeatherDashboard.AppHost` |
| **Purpose** | Defines all resources, containers, and service dependencies for Aspire orchestration |
| **Key File** | `AppHost.cs` |

The AppHost declares:
- **Redis** container (`redis`) — distributed cache
- **PostgreSQL** container (`postgres`) with database `preferencesdb`
- **Weather API** project — references Redis
- **Preferences API** project — references PostgreSQL
- **Frontend** project — references both APIs
- **Worker** project — references both APIs

All inter-service dependencies use `.WithReference()` for service discovery injection and `.WaitFor()` for startup ordering.

### 3.2 ServiceDefaults

| Attribute | Value |
|-----------|-------|
| **Project** | `WeatherDashboard.ServiceDefaults` |
| **Purpose** | Shared library providing OpenTelemetry, health checks, resilience handlers, and service discovery to all services |
| **Key File** | `Extensions.cs` |

Every service calls `builder.AddServiceDefaults()` which configures:
- **OpenTelemetry**: Logging (formatted messages + scopes), Metrics (ASP.NET Core, HttpClient, Runtime), Tracing (ASP.NET Core, HttpClient)
- **Health checks**: `/health` (readiness) and `/alive` (liveness, tagged `"live"`) — mapped only in Development
- **Service discovery**: Automatic resolution of `https+http://servicename` URIs
- **Resilience**: Standard resilience handler on all `HttpClient` instances (retries, circuit breaker, timeout)
- **Trace filtering**: Health check endpoints excluded from traces

### 3.3 Weather API

| Attribute | Value |
|-----------|-------|
| **Project** | `WeatherDashboard.WeatherApi` |
| **Purpose** | REST API that fetches weather data from OpenWeatherMap and caches results in Redis |
| **Key Files** | `Program.cs`, `Endpoints/WeatherEndpoints.cs`, `Services/OpenWeatherMapService.cs`, `Services/WeatherCacheService.cs`, `HealthChecks/OpenWeatherMapHealthCheck.cs` |
| **Dependencies** | Redis (via Aspire `AddRedisDistributedCache`), OpenWeatherMap external API |

**Behavior:**
- Exposes two endpoints under `/api/weather`
- Uses cache-aside pattern: check Redis first, call OpenWeatherMap on miss, cache result
- On external API failure, returns stale cached data if available; otherwise returns 503
- City names are normalized (lowercase, trimmed, spaces → hyphens) for cache key consistency
- Registers a custom health check (`OpenWeatherMapHealthCheck`) that pings the external API with a lightweight request
- Temperature data returned in both Fahrenheit and Celsius

### 3.4 Preferences API

| Attribute | Value |
|-----------|-------|
| **Project** | `WeatherDashboard.PreferencesApi` |
| **Purpose** | REST API for managing user city preferences, backed by PostgreSQL via EF Core |
| **Key Files** | `Program.cs`, `Endpoints/PreferencesEndpoints.cs`, `Models/UserPreference.cs`, `Data/PreferencesDbContext.cs` |
| **Dependencies** | PostgreSQL (via Aspire `AddNpgsqlDbContext`) |

**Behavior:**
- Full CRUD for user city preferences
- Automatic database migration on startup in Development mode
- Seed data: `demo-user` with cities Seattle, Portland, Austin
- Unique constraint on (UserId, City) — duplicate adds return 409 Conflict
- DisplayOrder tracked for deterministic ordering
- Registers a `DbContextCheck` health check for PostgreSQL connectivity

### 3.5 Frontend

| Attribute | Value |
|-----------|-------|
| **Project** | `WeatherDashboard.Frontend` |
| **Purpose** | Blazor Server UI for searching weather and managing city preferences |
| **Key Files** | `Program.cs`, `Components/Pages/Home.razor`, `Components/Pages/Forecast.razor`, `Services/WeatherApiClient.cs`, `Services/PreferencesApiClient.cs` |
| **Dependencies** | Weather API, Preferences API (via Aspire service discovery) |

**Behavior:**
- Uses Interactive Server render mode for real-time UI updates
- Typed `HttpClient` services configured with Aspire service discovery URIs (`https+http://weatherapi`, `https+http://preferencesapi`)
- Hardcoded user ID `demo-user` (no authentication)
- Skeleton loading states while data loads
- Error states with retry buttons
- Uses `UseStatusCodePagesWithReExecute` for 404 handling

### 3.6 Worker

| Attribute | Value |
|-----------|-------|
| **Project** | `WeatherDashboard.Worker` |
| **Purpose** | Background service that periodically refreshes weather data for all saved cities |
| **Key Files** | `Program.cs`, `Services/WeatherRefreshWorker.cs`, `Services/WeatherRefreshHealthCheck.cs` |
| **Dependencies** | Weather API, Preferences API (via Aspire service discovery) |

**Behavior:**
- Runs on a 15-minute `PeriodicTimer` cycle
- Fetches the distinct city list from Preferences API, then calls Weather API for each city (both current + forecast)
- 500ms stagger between cities to avoid API rate limiting
- Exponential backoff retry (up to 3 retries per city, starting at 1s)
- Custom health check tracks success/failure counts and last refresh timestamp
- 16-minute grace period on startup before health check reports unhealthy

---

## 4. Aspire Integrations

### 4.1 Redis Caching Strategy

| Aspect | Detail |
|--------|--------|
| **Aspire API** | `builder.AddRedisDistributedCache("redis")` in Weather API |
| **Cache interface** | `IDistributedCache` (Microsoft.Extensions.Caching.Distributed) |
| **Pattern** | Cache-aside with stale fallback on API failure |

**Cache Keys:**

| Key Pattern | Example | Data |
|-------------|---------|------|
| `weather:current:{normalized-city}` | `weather:current:seattle` | Serialized `CurrentWeather` JSON |
| `weather:forecast:{normalized-city}` | `weather:forecast:new-york` | Serialized `Forecast` JSON |

**City Normalization:** `city.Trim().ToLowerInvariant().Replace(' ', '-')`

**TTL:** 15 minutes (`AbsoluteExpirationRelativeToNow`), aligned with the Worker refresh cycle.

**Fallback Behavior:** If OpenWeatherMap returns an error and a stale cache entry exists (expired but still in Redis), the stale data is returned with a warning log.

### 4.2 PostgreSQL Data Model

| Aspire API | `builder.AddNpgsqlDbContext<PreferencesDbContext>("preferencesdb")` |
|------------|------|
| **Table** | `user_preferences` |
| **ORM** | Entity Framework Core with Npgsql provider |
| **Migration** | Auto-migrated on startup in Development (`db.Database.MigrateAsync()`) |

**Schema:**

| Column | Type | Constraints |
|--------|------|-------------|
| `Id` | `int` | Primary key, auto-increment |
| `UserId` | `string` | Indexed |
| `City` | `string` | — |
| `DisplayOrder` | `int` | User-defined ordering |
| `CreatedAt` | `DateTime` | Default: `now() at time zone 'utc'` |

**Indexes:**
- `IX_UserPreferences_UserId` — non-unique index on `UserId`
- `IX_UserPreferences_UserId_City` — **unique** composite index on (`UserId`, `City`)

**Seed Data:**

| Id | UserId | City | DisplayOrder |
|----|--------|------|-------------|
| 1 | demo-user | Seattle | 0 |
| 2 | demo-user | Portland | 1 |
| 3 | demo-user | Austin | 2 |

### 4.3 Service Discovery URIs

| Consumer | Target | URI |
|----------|--------|-----|
| Frontend | Weather API | `https+http://weatherapi` |
| Frontend | Preferences API | `https+http://preferencesapi` |
| Worker | Weather API | `https+http://weatherapi` |
| Worker | Preferences API | `https+http://preferencesapi` |
| Weather API | OpenWeatherMap | `https://api.openweathermap.org/` (external, not service-discovered) |

The `https+http://` scheme tells Aspire service discovery to prefer HTTPS but fall back to HTTP.

### 4.4 Connection Strings (Injected by Aspire)

| Name | Consumer | Backing Resource |
|------|----------|-----------------|
| `ConnectionStrings__redis` | Weather API | Redis container |
| `ConnectionStrings__preferencesdb` | Preferences API | PostgreSQL database |

---

## 5. Data Models

### 5.1 Weather API Models (`WeatherDashboard.WeatherApi.Models`)

**CurrentWeather** (record)

| Property | Type | Description |
|----------|------|-------------|
| `City` | `string` | City name (from API response) |
| `TemperatureF` | `double` | Temperature in Fahrenheit |
| `TemperatureC` | `double` | Temperature in Celsius (calculated: `(F - 32) * 5/9`) |
| `Conditions` | `string` | Weather description (e.g., "clear sky") |
| `Icon` | `string` | OpenWeatherMap icon code (e.g., "01d") |
| `Humidity` | `int` | Humidity percentage |
| `WindSpeedMph` | `double` | Wind speed in mph |
| `RetrievedAt` | `DateTime` | UTC timestamp when data was fetched |

**ForecastDay** (record)

| Property | Type | Description |
|----------|------|-------------|
| `Date` | `DateTime` | Date of the forecast day |
| `HighF` | `double` | High temperature (°F) |
| `LowF` | `double` | Low temperature (°F) |
| `HighC` | `double` | High temperature (°C) |
| `LowC` | `double` | Low temperature (°C) |
| `Conditions` | `string` | Weather description (from midday entry) |
| `Icon` | `string` | OpenWeatherMap icon code |

**Forecast** (record)

| Property | Type | Description |
|----------|------|-------------|
| `City` | `string` | City name |
| `Days` | `List<ForecastDay>` | Up to 5 daily forecast entries |
| `RetrievedAt` | `DateTime` | UTC timestamp |

### 5.2 Preferences API Models (`WeatherDashboard.PreferencesApi.Models`)

**UserPreference** (class, EF Core entity)

| Property | Type | Description |
|----------|------|-------------|
| `Id` | `int` | Auto-incremented primary key |
| `UserId` | `string` | User identifier |
| `City` | `string` | Saved city name |
| `DisplayOrder` | `int` | Sort order for display |
| `CreatedAt` | `DateTime` | Creation timestamp (UTC) |

**AddCityRequest** (record, request DTO)

| Property | Type | Description |
|----------|------|-------------|
| `City` | `string` | City name to add |

### 5.3 Frontend Models (`WeatherDashboard.Frontend.Models`)

The Frontend defines its own mirror DTOs for deserialization. These are structurally identical to the API models:

- `CurrentWeather` — mirrors `WeatherApi.Models.CurrentWeather`
- `Forecast` / `ForecastDay` — mirrors `WeatherApi.Models.Forecast` / `ForecastDay`
- `UserPreference` — mirrors `PreferencesApi.Models.UserPreference`

---

## 6. API Contracts

### 6.1 Weather API

Base path: `/api/weather`

---

#### `GET /api/weather/{city}`

Get current weather for a city. Results are cached in Redis for 15 minutes.

**Parameters:**

| Name | In | Type | Required | Description |
|------|----|------|----------|-------------|
| `city` | path | string | Yes | City name (URL-encoded) |

**Responses:**

| Status | Body | Condition |
|--------|------|-----------|
| `200 OK` | `CurrentWeather` JSON | Success (from cache or API) |
| `404 Not Found` | `{ "error": "City '{city}' not found." }` | OpenWeatherMap returns 404 |
| `503 Service Unavailable` | — | OpenWeatherMap unreachable and no cached data |

**200 Response Example:**
```json
{
  "city": "Seattle",
  "temperatureF": 62.5,
  "temperatureC": 16.9,
  "conditions": "overcast clouds",
  "icon": "04d",
  "humidity": 72,
  "windSpeedMph": 8.3,
  "retrievedAt": "2025-07-16T12:00:00Z"
}
```

---

#### `GET /api/weather/{city}/forecast`

Get 5-day weather forecast for a city. 3-hour intervals are aggregated into daily high/low.

**Parameters:**

| Name | In | Type | Required | Description |
|------|----|------|----------|-------------|
| `city` | path | string | Yes | City name (URL-encoded) |

**Responses:**

| Status | Body | Condition |
|--------|------|-----------|
| `200 OK` | `Forecast` JSON | Success |
| `404 Not Found` | `{ "error": "City '{city}' not found." }` | City not found |
| `503 Service Unavailable` | — | External API unreachable |

**200 Response Example:**
```json
{
  "city": "Seattle",
  "days": [
    {
      "date": "2025-07-16T00:00:00",
      "highF": 72.1,
      "lowF": 55.3,
      "highC": 22.3,
      "lowC": 12.9,
      "conditions": "light rain",
      "icon": "10d"
    }
  ],
  "retrievedAt": "2025-07-16T12:00:00Z"
}
```

---

### 6.2 Preferences API

Base path: `/api/preferences`

---

#### `GET /api/preferences/cities`

Get all distinct saved cities across all users, sorted alphabetically.

**Responses:**

| Status | Body |
|--------|------|
| `200 OK` | `string[]` — e.g., `["Austin", "Portland", "Seattle"]` |

---

#### `GET /api/preferences/{userId}`

Get all preferences for a specific user, ordered by `DisplayOrder`.

**Parameters:**

| Name | In | Type | Required |
|------|----|------|----------|
| `userId` | path | string | Yes |

**Responses:**

| Status | Body |
|--------|------|
| `200 OK` | `UserPreference[]` |

---

#### `POST /api/preferences/{userId}`

Add a city to a user's preferences. DisplayOrder is auto-assigned (max + 1).

**Parameters:**

| Name | In | Type | Required |
|------|----|------|----------|
| `userId` | path | string | Yes |

**Request Body:**
```json
{ "city": "Seattle" }
```

**Responses:**

| Status | Body | Condition |
|--------|------|-----------|
| `201 Created` | `UserPreference` | City added successfully |
| `409 Conflict` | `{ "message": "City '...' is already saved for user '...'." }` | Duplicate |

---

#### `DELETE /api/preferences/{userId}/{city}`

Remove a city from a user's preferences.

**Responses:**

| Status | Condition |
|--------|-----------|
| `204 No Content` | City removed |
| `404 Not Found` | Preference not found |

---

### 6.3 Health Endpoints (All Services)

| Endpoint | Purpose | Tags |
|----------|---------|------|
| `GET /health` | Full readiness check (all registered checks must pass) | — |
| `GET /alive` | Liveness check (only checks tagged `"live"`) | `live` |

Mapped only in Development environments (see `Extensions.MapDefaultEndpoints()`).

---

## 7. Frontend

### Pages

| Route | Component | Render Mode | Description |
|-------|-----------|-------------|-------------|
| `/` | `Home.razor` | Interactive Server | Main dashboard — displays weather cards for saved cities |
| `/forecast/{City}` | `Forecast.razor` | Interactive Server | 5-day forecast detail page for a specific city |
| `/Error` | `Error.razor` | Static | Error page with request ID |
| `/not-found` | `NotFound.razor` | Static | Custom 404 page |

### Home Page (`/`) — User Interactions

1. **View saved cities**: On load, fetches preferences for `demo-user`, then fetches current weather for each city in parallel. Displays as a responsive card grid.
2. **Add city**: Text input + "Add City" button (also supports Enter key). Calls Preferences API to save, then reloads dashboard. Shows warning on duplicate, error on failure.
3. **Remove city**: "✕" button on each card. Calls Preferences API to delete, then reloads.
4. **View forecast**: Clicking a weather card navigates to `/forecast/{city}`.
5. **Loading state**: Skeleton cards shown while data loads.
6. **Error state**: Message with "Retry" button if services are unreachable.
7. **Empty state**: Prompt to add cities if none are saved.

### Forecast Page (`/forecast/{City}`)

1. **View forecast**: Displays a table with Date, Icon, Conditions, High, Low for up to 5 days.
2. **Back navigation**: "Back to Dashboard" button returns to `/`.
3. **Loading/Error states**: Same pattern as Home page.

### UI Features

- Weather icons from OpenWeatherMap CDN (`openweathermap.org/img/wn/{icon}@2x.png`)
- Dual temperature display (°F / °C)
- Humidity and wind speed on each card
- Reconnect modal for Blazor Server SignalR disconnections (`ReconnectModal.razor`)
- Scoped CSS for layout components

### Service Clients

| Client | Base Address | Methods |
|--------|-------------|---------|
| `WeatherApiClient` | `https+http://weatherapi` | `GetCurrentWeatherAsync(city)`, `GetForecastAsync(city)` |
| `PreferencesApiClient` | `https+http://preferencesapi` | `GetUserPreferencesAsync(userId)`, `AddCityAsync(userId, city)`, `RemoveCityAsync(userId, city)` |

---

## 8. Background Worker

### WeatherRefreshWorker

| Parameter | Value |
|-----------|-------|
| **Refresh interval** | 15 minutes (`PeriodicTimer`) |
| **City stagger** | 500ms between cities |
| **Max retries** | 3 per city |
| **Retry strategy** | Exponential backoff (1s → 2s → 4s) |

**Refresh Cycle:**
1. Fetch distinct city list from `GET /api/preferences/cities`
2. For each city, call both `GET /api/weather/{city}` and `GET /api/weather/{city}/forecast`
3. Each call triggers the Weather API's cache-aside logic, refreshing Redis
4. Log success/failure per city and total cycle metrics

**Error Handling:**
- If city list fetch fails, logs error and records as a full failure
- Per-city failures are retried with exponential backoff
- Partial failures are tolerated — the cycle continues to the next city

### WeatherRefreshHealthCheck

Tracks the last refresh cycle result and reports health status:

| Condition | Status |
|-----------|--------|
| No refresh yet, within 16-minute grace period | `Healthy` |
| No refresh after grace period | `Unhealthy` |
| All cities succeeded | `Healthy` |
| Some cities failed | `Degraded` |
| All cities failed | `Unhealthy` |

Health check data includes `lastSuccessCount`, `lastFailureCount`, and `lastRefreshTime`.

---

## 9. Observability

### OpenTelemetry Configuration (via ServiceDefaults)

All services call `builder.AddServiceDefaults()` which configures:

| Signal | Instrumentation |
|--------|----------------|
| **Logging** | OpenTelemetry logging with formatted messages and scopes |
| **Metrics** | ASP.NET Core, HttpClient, .NET Runtime |
| **Tracing** | ASP.NET Core, HttpClient (health endpoints filtered out) |

Each service emits traces under its own `ApplicationName` as the `ActivitySource`.

### OTLP Export

- Automatically configured by Aspire via `OTEL_EXPORTER_OTLP_ENDPOINT` environment variable
- All traces, metrics, and logs are exported to the Aspire Dashboard's OTLP collector
- W3C `traceparent`/`tracestate` headers propagated through `HttpClient`

### Expected Trace Paths

| Path | Trigger |
|------|---------|
| Frontend → Weather API → Redis → OpenWeatherMap | User searches for a city |
| Frontend → Preferences API → PostgreSQL | User adds/removes a city |
| Worker → Preferences API → PostgreSQL | Worker fetches city list |
| Worker → Weather API → Redis → OpenWeatherMap | Worker refreshes weather data |

### Health Checks

| Service | Check Name | Type | What It Validates |
|---------|-----------|------|-------------------|
| All services | `self` | Liveness (`live` tag) | App is responsive |
| Weather API | `openweathermap` | Readiness | OpenWeatherMap API is reachable (lightweight GET for London) |
| Weather API | (auto) | Readiness | Redis connectivity (auto-registered by Aspire Redis component) |
| Preferences API | `preferencesdb` | Readiness | PostgreSQL connectivity (EF Core `DbContextCheck`) |
| Worker | `weather-refresh` | Readiness | Last refresh cycle succeeded (with grace period) |

### Structured Logging

- All services use `ILogger<T>` with structured log messages
- Logs include trace correlation IDs (`TraceId`, `SpanId`) for distributed trace linkage
- Key log events:
  - Cache hits/misses (`WeatherCacheService`)
  - Refresh cycle start/completion/failure (`WeatherRefreshWorker`)
  - Per-city refresh timing and retry attempts

---

## 10. Testing

### Test Framework

| Aspect | Detail |
|--------|--------|
| **Framework** | xUnit |
| **Hosting** | `Aspire.Hosting.Testing` — boots the full distributed application |
| **Fixture** | `AspireAppFixture` — shared across all test classes via `[Collection("AspireApp")]` |
| **Startup timeout** | 120 seconds per resource |
| **Prerequisites** | Docker Desktop (for Redis/PostgreSQL containers) |

### Test Fixture (`AspireAppFixture.cs`)

Boots the complete Aspire app including:
- Redis and PostgreSQL containers
- Weather API, Preferences API, Frontend, Worker
- Waits for `weatherapi` and `preferencesapi` to be healthy before running tests
- Adds standard resilience handler to test HTTP clients
- Shared across all test classes — app starts once per test run

### Test Files

#### `WeatherApiTests.cs` (4 tests)

| Test | Validates |
|------|-----------|
| `GetCurrentWeather_ReturnsValidJson` | Endpoint routing + response structure (city, temperatureF, temperatureC, conditions, humidity, windSpeedMph, retrievedAt) |
| `GetForecast_ReturnsValidJson` | Forecast endpoint routing + response structure (city, days array, retrievedAt) |
| `GetCurrentWeather_NonsenseCity_ReturnsNotFoundOr503` | 404 for invalid city or 503/500 if API key missing |
| `GetCurrentWeather_CachingWorks_SecondCallSucceeds` | Two calls return same status; cached response has same city name |

#### `PreferencesApiTests.cs` (5 tests)

| Test | Validates |
|------|-----------|
| `GetUserPreferences_ReturnsSeededCities` | Seeded data for `demo-user` (Seattle, Portland, Austin) |
| `PostPreference_AddsNewCity` | POST creates preference, GET confirms persistence |
| `PostPreference_DuplicateCity_Returns409` | Duplicate city returns 409 Conflict |
| `DeletePreference_RemovesCity` | DELETE removes preference, GET confirms removal |
| `GetAllCities_ReturnsDistinctList` | `/cities` returns distinct, sorted list |

#### `ServiceDiscoveryTests.cs` (5 tests)

| Test | Validates |
|------|-----------|
| `AllResources_AreRunning` | HTTP clients can be created for weatherapi, preferencesapi, frontend |
| `Frontend_CanReachWeatherApi_ViaServiceDiscovery` | `/alive` on Weather API returns 200 |
| `Frontend_CanReachPreferencesApi_ViaServiceDiscovery` | `/alive` on Preferences API returns 200 |
| `Worker_IsRunning` | Worker resource exists in `DistributedApplicationModel` |
| `NoHardcodedUrls_InServiceConfiguration` | No `http://localhost:` or `https://localhost:` in Frontend or Worker source files |

#### `ResilienceTests.cs` (4 tests)

| Test | Validates |
|------|-----------|
| `WeatherApi_HandlesRequestGracefully` | Request completes within 60s, returns valid status |
| `CachingBehavior_SecondRequestUsesCache` | Second call returns same `retrievedAt` (proving cache hit) |
| `WeatherApi_Returns503_WhenExternalApiUnavailable_AndNoCacheExists` | Unique city with no cache returns 404/503/500 |
| `PreferencesApi_IsResilient_UnderLoad` | 5 concurrent requests all return 200 |

### Test Coverage Summary

| Area | Coverage |
|------|----------|
| Weather API endpoints | ✅ Current weather, forecast, error cases, caching |
| Preferences API CRUD | ✅ Read, create, duplicate detection, delete, distinct cities |
| Service discovery | ✅ All services reachable, no hardcoded URLs |
| Resilience | ✅ Graceful degradation, caching, concurrent load |
| Health checks | ✅ Implicitly tested (fixture waits for healthy status) |
| Frontend UI | ❌ No UI tests (Blazor component testing not included) |
| Worker behavior | ⚠️ Partial (resource exists, but refresh cycle not directly tested) |

---

## 11. Work Items

The project was implemented across 19 GitHub issues organized in four phases.

### Phase 1: Foundation

| # | Title | Description |
|---|-------|-------------|
| 1 | Solution scaffolding | Create the .NET solution with all six projects (AppHost, ServiceDefaults, WeatherApi, PreferencesApi, Frontend, Worker) |
| 2 | ServiceDefaults setup | Configure OpenTelemetry (logging, metrics, tracing), health check endpoints, service discovery, and standard resilience handlers |
| 3 | AppHost wiring | Define Redis/PostgreSQL containers, wire all project references with `WithReference`/`WaitFor` dependency chains |

### Phase 2: Core Services

| # | Title | Description |
|---|-------|-------------|
| 4 | Weather API — OpenWeatherMap service | Implement `OpenWeatherMapService` with `HttpClient`, parse JSON responses, convert temperatures to F/C |
| 5 | Weather API — Redis caching | Implement `WeatherCacheService` with cache-aside pattern, 15-min TTL, stale fallback on API failure |
| 6 | Weather API — Endpoints | Create minimal API endpoints: `GET /api/weather/{city}`, `GET /api/weather/{city}/forecast` |
| 7 | Weather API — Health check | Implement `OpenWeatherMapHealthCheck` to verify external API reachability |
| 8 | Preferences API — Data model | Define `UserPreference` entity, `PreferencesDbContext` with table mapping, indexes, unique constraint, seed data |
| 9 | Preferences API — Endpoints | Create CRUD endpoints: GET cities, GET user preferences, POST add city, DELETE remove city |
| 10 | Preferences API — Health check | Register `DbContextCheck` for PostgreSQL connectivity |
| 11 | Preferences API — Auto-migration | Enable `Database.MigrateAsync()` on startup in Development environment |

### Phase 3: Frontend & Worker

| # | Title | Description |
|---|-------|-------------|
| 12 | Frontend — Service clients | Implement `WeatherApiClient` and `PreferencesApiClient` typed HttpClients with Aspire service discovery URIs |
| 13 | Frontend — Dashboard page | Build `Home.razor` with weather card grid, add/remove cities, loading/error/empty states, keyboard support |
| 14 | Frontend — Forecast page | Build `Forecast.razor` with 5-day forecast table, back navigation, loading/error states |
| 15 | Worker — Background refresh | Implement `WeatherRefreshWorker` with 15-min cycle, staggered requests, exponential backoff retry |
| 16 | Worker — Health check | Implement `WeatherRefreshHealthCheck` with grace period, success/failure tracking, degraded status |

### Phase 4: Integration & Testing

| # | Title | Description |
|---|-------|-------------|
| 17 | Integration tests — Fixture | Create `AspireAppFixture` that boots the full Aspire app with all containers and waits for healthy services |
| 18 | Integration tests — API + resilience | Write Weather API tests (endpoints, caching, error handling), Preferences API tests (CRUD, duplicates), service discovery tests, resilience tests |
| 19 | Documentation | Write telemetry verification guide (`telemetry-verification.md`), README, and PRD |

---

## Appendix: Configuration Reference

### User Secrets

| Key | Project | Required | Description |
|-----|---------|----------|-------------|
| `OpenWeatherMap:ApiKey` | WeatherApi | Yes | Free-tier API key from openweathermap.org |

### Environment Variables (Set Automatically by Aspire)

| Variable | Description |
|----------|-------------|
| `OTEL_EXPORTER_OTLP_ENDPOINT` | OTLP collector endpoint for OpenTelemetry |
| `ConnectionStrings__redis` | Redis connection string |
| `ConnectionStrings__preferencesdb` | PostgreSQL connection string |

### Prerequisites

| Dependency | Version | Purpose |
|-----------|---------|---------|
| .NET SDK | 10.0+ | Build and run |
| Docker Desktop | Latest | Redis and PostgreSQL containers |
| OpenWeatherMap API key | Free tier | External weather data |
