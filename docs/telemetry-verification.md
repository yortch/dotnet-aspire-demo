# OpenTelemetry Trace Verification

This document describes the expected distributed trace paths across the WeatherDashboard services and how to verify them using the .NET Aspire dashboard.

## Expected Trace Paths

### 1. Frontend → Weather API → Redis

| Step | Service | Operation | Details |
|------|---------|-----------|---------|
| 1 | **Frontend** | HTTP GET | User requests weather data via Blazor UI |
| 2 | **Weather API** | HTTP GET `/api/weather/{city}` | Receives request via service discovery (`https+http://weatherapi`) |
| 3 | **Weather API** | Redis GET | `WeatherCacheService` checks Redis distributed cache |
| 4 | **Weather API** | HTTP GET (OpenWeatherMap) | On cache miss, calls external API |
| 5 | **Weather API** | Redis SET | Caches response in Redis |

### 2. Frontend → Preferences API → PostgreSQL

| Step | Service | Operation | Details |
|------|---------|-----------|---------|
| 1 | **Frontend** | HTTP GET/POST/DELETE | User manages city preferences via Blazor UI |
| 2 | **Preferences API** | HTTP endpoint `/api/preferences/*` | Receives request via service discovery (`https+http://preferencesapi`) |
| 3 | **Preferences API** | PostgreSQL query | EF Core executes SQL against `preferencesdb` |

### 3. Worker → Weather API → Redis

| Step | Service | Operation | Details |
|------|---------|-----------|---------|
| 1 | **Worker** | Background refresh | `WeatherRefreshWorker` periodically fetches weather data |
| 2 | **Weather API** | HTTP GET `/api/weather/{city}` | Receives request via service discovery (`https+http://weatherapi`) |
| 3 | **Weather API** | Redis GET/SET | Cache lookup and update |

### 4. Worker → Preferences API

| Step | Service | Operation | Details |
|------|---------|-----------|---------|
| 1 | **Worker** | HTTP GET | Fetches list of saved cities to refresh |
| 2 | **Preferences API** | HTTP GET `/api/preferences/cities` | Returns distinct cities from PostgreSQL |

## ServiceDefaults OpenTelemetry Configuration

The `WeatherDashboard.ServiceDefaults/Extensions.cs` file configures OpenTelemetry for all services via `AddServiceDefaults()`. Each service project references ServiceDefaults and calls this method in `Program.cs`.

### What is configured

| Feature | Configuration | Status |
|---------|--------------|--------|
| **Logging** | `AddOpenTelemetry()` with `IncludeFormattedMessage` and `IncludeScopes` | ✅ Configured |
| **Metrics** | ASP.NET Core, HttpClient, and Runtime instrumentation | ✅ Configured |
| **Tracing** | ASP.NET Core and HttpClient instrumentation | ✅ Configured |
| **Trace source** | `AddSource(builder.Environment.ApplicationName)` — each service emits traces under its own name | ✅ Configured |
| **Health check filtering** | `/health` and `/alive` endpoints excluded from traces | ✅ Configured |
| **OTLP exporter** | Conditionally enabled via `OTEL_EXPORTER_OTLP_ENDPOINT` (set automatically by Aspire) | ✅ Configured |
| **Service discovery** | Configured with resilience handlers | ✅ Configured |

### Trace propagation

.NET Aspire automatically sets `OTEL_EXPORTER_OTLP_ENDPOINT` for all orchestrated services, so traces from every service are exported to the Aspire dashboard's OTLP collector. W3C `traceparent`/`tracestate` headers are propagated by default through `HttpClient`, enabling distributed trace correlation.

### Packages included (ServiceDefaults.csproj)

- `OpenTelemetry.Exporter.OpenTelemetryProtocol` 1.14.0
- `OpenTelemetry.Extensions.Hosting` 1.14.0
- `OpenTelemetry.Instrumentation.AspNetCore` 1.14.0
- `OpenTelemetry.Instrumentation.Http` 1.14.0
- `OpenTelemetry.Instrumentation.Runtime` 1.14.0

## How to Verify Traces in the Aspire Dashboard

### Prerequisites

- .NET 10 SDK installed
- Docker Desktop running (for Redis and PostgreSQL containers)
- OpenWeatherMap API key configured

### Steps

1. **Start the application:**
   ```bash
   dotnet run --project WeatherDashboard.AppHost
   ```

2. **Open the Aspire dashboard:**
   The terminal output will display a URL like `https://localhost:17178`. Open it in your browser.

3. **Generate trace activity:**
   - Open the Frontend URL shown in the dashboard (typically `https://localhost:7xxx`).
   - Search for a city to trigger the **Frontend → Weather API → Redis** path.
   - Add/remove cities from preferences to trigger the **Frontend → Preferences API → PostgreSQL** path.
   - Wait for the Worker's background refresh cycle to trigger **Worker → Weather API** and **Worker → Preferences API** paths.

4. **View traces in the dashboard:**
   - Navigate to the **Traces** tab in the Aspire dashboard.
   - Look for traces originating from `frontend`, `weatherapi`, `preferencesapi`, and `worker`.
   - Click on a trace to see the full span waterfall showing cross-service calls.

5. **Verify distributed correlation:**
   - Each trace should show a connected span tree across multiple services.
   - Frontend-initiated traces should show child spans in Weather API or Preferences API.
   - Worker-initiated traces should show child spans in Weather API and Preferences API.
   - Redis and PostgreSQL operations should appear as leaf spans.

6. **Check metrics:**
   - Navigate to the **Metrics** tab.
   - Verify that HTTP request metrics, runtime metrics, and custom metrics are reported by each service.

7. **Check structured logs:**
   - Navigate to the **Structured Logs** tab.
   - Verify that logs include trace correlation IDs (TraceId, SpanId).
   - Filter by service name to see per-service logs.

## Gaps and Recommendations

| Area | Status | Recommendation |
|------|--------|----------------|
| **Redis instrumentation** | ⚠️ Implicit | Redis traces are provided by the Aspire Redis component (`Aspire.StackExchange.Redis`), not by an explicit OpenTelemetry instrumentation package in ServiceDefaults. This works correctly but is worth noting. |
| **PostgreSQL instrumentation** | ⚠️ Implicit | EF Core / Npgsql traces are provided by the Aspire PostgreSQL component (`Aspire.Npgsql.EntityFrameworkCore`). No additional OTel package needed. |
| **Custom activity sources** | ℹ️ Optional | Consider adding custom `ActivitySource` instances in business-critical services (e.g., `WeatherCacheService`) for finer-grained trace visibility. |
| **gRPC instrumentation** | ℹ️ Commented out | gRPC instrumentation is available but commented out in `Extensions.cs`. Enable if gRPC is introduced later. |
| **Azure Monitor** | ℹ️ Commented out | Azure Monitor exporter is commented out. Enable for production deployments by adding the `Azure.Monitor.OpenTelemetry.AspNetCore` package and setting `APPLICATIONINSIGHTS_CONNECTION_STRING`. |
| **Sampling** | ℹ️ Not configured | No trace sampling is configured. For production, consider adding a sampling strategy to reduce trace volume. |
