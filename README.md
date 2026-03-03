# 🌤️ WeatherDashboard

[![.NET](https://img.shields.io/badge/.NET-10.0-512BD4)](https://dotnet.microsoft.com/)
[![Aspire](https://img.shields.io/badge/.NET%20Aspire-Orchestrated-blueviolet)](https://learn.microsoft.com/dotnet/aspire/)
[![License](https://img.shields.io/badge/license-MIT-green)](LICENSE)

A real-time weather dashboard built with **.NET Aspire**, featuring a Blazor Server frontend, dual REST APIs backed by Redis and PostgreSQL, and a background worker for automatic data refresh — all wired together with Aspire's service discovery, health checks, and OpenTelemetry observability.

## Architecture Overview

The solution is composed of six projects orchestrated by .NET Aspire:

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

| Project | Description |
|---------|-------------|
| **WeatherDashboard.AppHost** | .NET Aspire orchestrator — defines all services, containers, and their dependencies |
| **WeatherDashboard.Frontend** | Blazor Server UI for searching weather and managing city preferences |
| **WeatherDashboard.WeatherApi** | REST API that fetches weather data from OpenWeatherMap and caches results in Redis |
| **WeatherDashboard.PreferencesApi** | REST API for managing user city preferences, backed by PostgreSQL via EF Core |
| **WeatherDashboard.Worker** | Background service that periodically refreshes weather data for saved cities |
| **WeatherDashboard.ServiceDefaults** | Shared configuration for OpenTelemetry, health checks, resilience, and service discovery |

## Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)
- [Docker Desktop](https://www.docker.com/products/docker-desktop/) (for Redis and PostgreSQL containers managed by Aspire)
- [OpenWeatherMap API key](https://openweathermap.org/api) (free tier is sufficient)

## Getting Started

1. **Clone the repository:**
   ```bash
   git clone https://github.com/yortch/dotnet-aspire-demo.git
   cd dotnet-aspire-demo
   ```

2. **Set your OpenWeatherMap API key:**

   Get a free API key by signing up at [openweathermap.org/api](https://openweathermap.org/api), then go to **My API Keys** to copy your key.

   ```bash
   dotnet user-secrets init --project WeatherDashboard.WeatherApi
   dotnet user-secrets set "OpenWeatherMap:ApiKey" "YOUR_KEY" --project WeatherDashboard.WeatherApi
   ```

3. **Run the application:**

   Make sure **Docker Desktop is running** before starting — Aspire uses it to spin up the Redis and PostgreSQL containers.

   ```bash
   dotnet run --project WeatherDashboard.AppHost
   ```

4. **Open the Aspire dashboard:**
   The terminal will display a dashboard URL (e.g., `https://localhost:17178`). Open it in your browser to see all services, traces, metrics, and logs.

5. **Use the app:**
   Click the Frontend URL shown in the Aspire dashboard to open the weather dashboard.

## Project Structure

```
dotnet-aspire-demo/
├── WeatherDashboard.AppHost/          # Aspire orchestrator
│   └── AppHost.cs                     # Service and container definitions
├── WeatherDashboard.Frontend/         # Blazor Server UI
│   ├── Components/                    # Razor components
│   └── Services/                      # API client services
├── WeatherDashboard.WeatherApi/       # Weather REST API
│   ├── Endpoints/                     # Minimal API endpoints
│   ├── Services/                      # OpenWeatherMap + cache services
│   └── HealthChecks/                  # Custom health checks
├── WeatherDashboard.PreferencesApi/   # Preferences REST API
│   ├── Endpoints/                     # Minimal API endpoints
│   ├── Data/                          # EF Core DbContext and migrations
│   └── Models/                        # Data models
├── WeatherDashboard.Worker/           # Background refresh service
│   └── Services/                      # Worker and health check services
├── WeatherDashboard.ServiceDefaults/  # Shared Aspire defaults
│   └── Extensions.cs                  # OpenTelemetry, health checks, resilience
└── docs/                              # Documentation
```

## API Endpoints

### Weather API

| Method | Endpoint | Description |
|--------|----------|-------------|
| `GET` | `/api/weather/{city}` | Get current weather for a city (cached in Redis) |
| `GET` | `/api/weather/{city}/forecast` | Get weather forecast for a city (cached in Redis) |

### Preferences API

| Method | Endpoint | Description |
|--------|----------|-------------|
| `GET` | `/api/preferences/cities` | Get all distinct saved cities |
| `GET` | `/api/preferences/{userId}` | Get preferences for a specific user |
| `POST` | `/api/preferences/{userId}` | Add a city to a user's preferences (body: `{ "city": "Seattle" }`) |
| `DELETE` | `/api/preferences/{userId}/{city}` | Remove a city from a user's preferences |

### Health Endpoints (all services)

| Endpoint | Description |
|----------|-------------|
| `/health` | Full readiness check |
| `/alive` | Liveness check |

## Configuration

### User Secrets

| Key | Project | Description |
|-----|---------|-------------|
| `OpenWeatherMap:ApiKey` | WeatherApi | API key for OpenWeatherMap (required) |

### Environment Variables (set automatically by Aspire)

| Variable | Description |
|----------|-------------|
| `OTEL_EXPORTER_OTLP_ENDPOINT` | OpenTelemetry collector endpoint |
| `ConnectionStrings__redis` | Redis connection string |
| `ConnectionStrings__preferencesdb` | PostgreSQL connection string |

## Testing

Run integration tests (if available) with:

```bash
dotnet test
```

For manual verification of the full system:

1. Start the AppHost (`dotnet run --project WeatherDashboard.AppHost`).
2. Use the Aspire dashboard to verify all services are healthy.
3. Exercise the Frontend UI and check traces in the dashboard.

See [docs/telemetry-verification.md](docs/telemetry-verification.md) for detailed OpenTelemetry trace verification steps.

## Architecture Decisions

Design decisions and the product requirements document are maintained in:

- [docs/PRD.md](docs/PRD.md) — Product Requirements Document

## Built With

- [.NET Aspire](https://learn.microsoft.com/dotnet/aspire/) — Cloud-native orchestration and service defaults
- [Blazor Server](https://learn.microsoft.com/aspnet/core/blazor/) — Interactive server-side UI
- [ASP.NET Core Minimal APIs](https://learn.microsoft.com/aspnet/core/fundamentals/minimal-apis) — Lightweight REST endpoints
- [Redis](https://redis.io/) — Distributed caching for weather data
- [PostgreSQL](https://www.postgresql.org/) — Persistent storage for user preferences
- [Entity Framework Core](https://learn.microsoft.com/ef/core/) — ORM for PostgreSQL
- [OpenTelemetry](https://opentelemetry.io/) — Distributed tracing, metrics, and logging
- [OpenWeatherMap API](https://openweathermap.org/api) — Weather data provider
