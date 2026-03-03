# Weather Dashboard — .NET Aspire

## Project Context

- **User:** Jorge Balderas
- **Stack:** .NET 8/9, Aspire, Blazor Server, ASP.NET Core Web API, Redis, PostgreSQL, Worker Service
- **Description:** Real-time weather dashboard using .NET Aspire. AppHost orchestrates all services. Blazor Server frontend displays current conditions and 5-day forecast for saved cities. Weather API wraps OpenWeatherMap with Redis caching. User Preferences service stores saved cities per user in PostgreSQL. Background worker refreshes cached weather data every 15 minutes. All services use Aspire service discovery (no hardcoded URLs). Health checks and OpenTelemetry tracing via Aspire defaults.

## Members

| Name | Role | Scope | Emoji |
|------|------|-------|-------|
| Holden | AppHost Lead | AppHost project, service discovery wiring, `WithReference`/`WaitFor` orchestration, Aspire defaults (health checks, OpenTelemetry) | 🏗️ |
| Naomi | Redis Dev | Redis caching integration (`Aspire.Hosting.Redis`, `Aspire.StackExchangeRedis`), `IDistributedCache`, weather data cache layer | 🔴 |
| Amos | PostgreSQL Dev | PostgreSQL integration (`Aspire.Hosting.PostgreSQL`, `Aspire.Npgsql.EntityFrameworkCore`), EF Core, user preferences data model | 🐘 |
| Drummer | Frontend Dev | Blazor Server dashboard, UI components, current conditions display, 5-day forecast, city management UI | ⚛️ |
| Alex | Worker Dev | Background worker service (Aspire Worker template), scheduled 15-minute refresh, service-to-service calls | ⚙️ |
| Bobbie | Tester | End-to-end tests, service discovery validation, integration tests, data flow verification | 🧪 |
| Scribe | Session Logger | — | 📋 |
| Ralph | Work Monitor | — | 🔄 |
