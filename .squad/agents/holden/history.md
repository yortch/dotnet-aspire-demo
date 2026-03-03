# Holden — History

## Project Context
- **Project:** Real-time weather dashboard using .NET Aspire
- **User:** Jorge Balderas
- **Stack:** .NET, Aspire, Blazor Server, ASP.NET Core Web API, Redis, PostgreSQL, Worker Service
- **My Role:** AppHost Lead — I own the orchestration layer and service discovery wiring

## Learnings
_Append new learnings below this line._
- PRD (`docs/PRD.md`) was regenerated from the fully implemented codebase. All sections derived from actual source files — models, endpoints, services, AppHost wiring, test files. No invented features. Covers all 19 work items across 4 phases.
- **Tracing fix (Aspire 13.1.2 / OTel 1.14.0):** The default Aspire ServiceDefaults template gates `UseOtlpExporter()` behind a `builder.Configuration["OTEL_EXPORTER_OTLP_ENDPOINT"]` check. In practice, this can fail to detect the env var the AppHost injects, causing zero traces in the dashboard. Fix: call `UseOtlpExporter()` unconditionally — it reads `OTEL_EXPORTER_OTLP_ENDPOINT` and `OTEL_EXPORTER_OTLP_PROTOCOL` from env vars at runtime independently. Aspire's `OtlpConfigurationExtensions.cs` always sets both vars (with gRPC preferred, defaulting to `http://localhost:18889`). All 4 services (WeatherApi, PreferencesApi, Frontend, Worker) already call `AddServiceDefaults()`. GitHub issue dotnet/aspire#12928 confirms this is a known Aspire 13 telemetry gap.
