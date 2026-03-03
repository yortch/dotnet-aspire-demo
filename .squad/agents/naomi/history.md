# Naomi — History

## Project Context
- **Project:** Real-time weather dashboard using .NET Aspire
- **User:** Jorge Balderas
- **Stack:** .NET, Aspire, Redis, OpenWeatherMap API
- **My Role:** Redis Dev — I own the caching layer for weather data

## Learnings
_Append new learnings below this line._

- **Redis TLS self-signed cert fix (Aspire containers) — SUPERSEDED:** The `configureOptions` callback approach (`options.CertificateValidation += ...`) does NOT reliably work. The callback may fire too late or Aspire's internal connection pipeline may bypass it. See v2 fix below.

- **Redis TLS fix v2 — WithoutHttpsCertificate (CORRECT FIX):** The proper fix is on the **AppHost side**, not the client. Use `builder.AddRedis("redis").WithoutHttpsCertificate()` in the AppHost to disable TLS on the Redis container entirely. This means Redis runs plain TCP, the connection string omits `ssl=True`, and no certificate validation is needed. The client-side `AddRedisDistributedCache("redis")` stays clean with no callbacks. Requires suppressing experimental diagnostic `ASPIRECERTIFICATES001` in the AppHost csproj (`<NoWarn>$(NoWarn);ASPIRECERTIFICATES001</NoWarn>`). This is the correct approach for local dev containers — production Redis should use proper certificates.

- **Aspire 13.1.2 API notes:** `AddRedisDistributedCache` has both `configureSettings` (for `StackExchangeRedisSettings` — has `ConnectionString`, `DisableHealthChecks`, `DisableTracing`) and `configureOptions` (for `ConfigurationOptions`). `StackExchangeRedisSettings` does NOT have a `ConfigurationOptions` property. `RedisResource` has a settable `TlsEnabled` property. `WithoutHttpsCertificate()` is a generic extension on `ResourceBuilderExtensions` that works on any resource builder.
