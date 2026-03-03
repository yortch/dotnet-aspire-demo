# Copilot Instructions — Weather Dashboard (.NET Aspire)

## Project Overview

This is a real-time weather dashboard built with .NET Aspire. The AppHost orchestrates all services using Aspire service discovery — no hardcoded URLs.

## Architecture

| Project | Purpose |
|---------|---------|
| `WeatherDashboard.AppHost` | Aspire AppHost — orchestrates all services and resources |
| `WeatherDashboard.ServiceDefaults` | Shared OpenTelemetry, health checks, resilience, service discovery |
| `WeatherDashboard.WeatherApi` | Wraps OpenWeatherMap API with Redis caching (15-min TTL) |
| `WeatherDashboard.PreferencesApi` | Stores saved cities per user in PostgreSQL via EF Core |
| `WeatherDashboard.Frontend` | Blazor Server dashboard — current conditions + 5-day forecast |
| `WeatherDashboard.Worker` | Background worker refreshing cached weather data every 15 minutes |

## Key Patterns

- **Service discovery:** Services reference each other by name (`http://weatherapi`, `http://preferencesapi`). Never hardcode URLs or ports.
- **Cache keys:** `weather:current:{city}` and `weather:forecast:{city}` — city names normalized to lowercase with hyphens.
- **ServiceDefaults:** Every service calls `builder.AddServiceDefaults()` and `app.MapDefaultEndpoints()`.
- **Worker pattern:** Worker calls API endpoints (not databases directly) to trigger cache refreshes.
- **Graceful degradation:** Weather API returns stale cache when OpenWeatherMap is unreachable.

## Coding Standards

- Use minimal API style for Web API endpoints.
- Use `IDistributedCache` for Redis interactions (not raw StackExchange.Redis).
- Use EF Core code-first with migrations for PostgreSQL.
- Every public API endpoint should have XML doc comments.
- Keep each file focused — one concern per file.
- Run `dotnet build` on the solution before committing to verify compilation.

## Branch Naming

Use `copilot/{issue-number}-{brief-slug}` for branches (e.g., `copilot/4-weather-api-endpoints`).

## PRD Reference

Full requirements are in `docs/PRD.md`. Always consult the PRD for API contracts, data models, and acceptance criteria.
