# Decision: Disable TLS on Aspire Redis Container (v2 Fix)

**Author:** Naomi (Redis Dev)
**Date:** 2025-07-22
**Status:** Applied

## Context

The Redis container in Aspire 13.1.2 uses a self-signed TLS certificate by default. StackExchange.Redis rejects it with `UntrustedRoot`. The previous fix (v1) used a `configureOptions` callback on `AddRedisDistributedCache` to bypass certificate validation — this compiled but **did not resolve the runtime error**.

## Decision

Disable TLS entirely on the Redis container from the AppHost using `WithoutHttpsCertificate()`:

**AppHost.cs:**
```csharp
var redis = builder.AddRedis("redis").WithoutHttpsCertificate();
```

**AppHost.csproj** (suppress experimental API diagnostic):
```xml
<NoWarn>$(NoWarn);ASPIRECERTIFICATES001</NoWarn>
```

**WeatherApi/Program.cs** (simplified — no callback needed):
```csharp
builder.AddRedisDistributedCache("redis");
```

## Why Not configureOptions?

The `configureOptions` callback on `AddRedisDistributedCache` sets `CertificateValidation` on `ConfigurationOptions`, but Aspire's internal connection pipeline (including health checks and the connection multiplexer setup) may not honor it reliably. The fix must happen at the container level, not the client level.

## Trade-offs

- `WithoutHttpsCertificate()` is marked experimental (`ASPIRECERTIFICATES001`) — may change in future Aspire versions
- TLS is disabled for local dev only; production Redis should use proper certificates
- This approach is cleaner: no client-side workarounds, no callback hacks

## Files Changed

1. `WeatherDashboard.AppHost/AppHost.cs` — added `.WithoutHttpsCertificate()`
2. `WeatherDashboard.AppHost/WeatherDashboard.AppHost.csproj` — added `ASPIRECERTIFICATES001` to `<NoWarn>`
3. `WeatherDashboard.WeatherApi/Program.cs` — removed `configureOptions` callback, back to clean `AddRedisDistributedCache("redis")`
