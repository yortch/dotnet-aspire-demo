# Decision: Make OTLP Exporter Unconditional

**Author:** Holden (AppHost Lead)
**Date:** 2025-07-18
**Status:** Implemented

## Context
Traces were completely absent from the Aspire dashboard despite all 4 services (WeatherApi, PreferencesApi, Frontend, Worker) correctly calling `AddServiceDefaults()` → `ConfigureOpenTelemetry()`.

## Root Cause
The `AddOpenTelemetryExporters()` method in `ServiceDefaults/Extensions.cs` gated `UseOtlpExporter()` behind:
```csharp
var useOtlpExporter = !string.IsNullOrWhiteSpace(builder.Configuration["OTEL_EXPORTER_OTLP_ENDPOINT"]);
```
This reads from the .NET `IConfiguration` chain at service build time. While the Aspire AppHost **does** inject `OTEL_EXPORTER_OTLP_ENDPOINT` into child processes (via `OtlpConfigurationExtensions.RegisterOtlpEnvironment`), the configuration-time read can fail to surface the env var, causing the conditional to evaluate to `false` and silently skip exporter registration.

## Decision
Removed the conditional gate. `UseOtlpExporter()` is now called unconditionally. The method natively reads `OTEL_EXPORTER_OTLP_ENDPOINT` and `OTEL_EXPORTER_OTLP_PROTOCOL` from environment variables at runtime through the OpenTelemetry SDK's own configuration mechanism.

## Risk Assessment
- **With Aspire AppHost:** Env vars are always injected — no behavior change, traces now flow.
- **Standalone (no Aspire):** Exporter defaults to `http://localhost:4317` (gRPC). If no collector is listening, the SDK retries gracefully with no crashes or user-visible errors.

## References
- [dotnet/aspire#12928](https://github.com/dotnet/aspire/issues/12928) — Same symptom after Aspire 13 upgrade
- Aspire source: `OtlpConfigurationExtensions.cs` / `OtlpEndpointResolver.cs` — confirms env var injection pipeline
- Commit: `fe69925`

## Team Impact
- No service code changes needed — all services already use `AddServiceDefaults()`
- Dashboard should now show traces, metrics, and structured logs from all 4 services
