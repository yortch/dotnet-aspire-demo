# Decision: Accept Aspire Redis Container Self-Signed TLS Certificates

**Author:** Naomi (Redis Dev)
**Date:** 2025-07-17
**Status:** Applied

## Context
Aspire's `AddRedis("redis")` spins up a Redis container with TLS enabled using a self-signed certificate. The StackExchange.Redis client rejects this cert by default, throwing `RedisConnectionException` with `UntrustedRoot`.

## Decision
Added a `CertificateValidation` callback to `AddRedisDistributedCache` in the WeatherApi that returns `true` for all certificates. This is scoped to the Aspire-orchestrated local dev environment.

## Change
**File:** `WeatherDashboard.WeatherApi/Program.cs`
```csharp
builder.AddRedisDistributedCache("redis", configureOptions: options =>
{
    options.CertificateValidation += (_, _, _, _) => true;
});
```

## Risk
- This bypasses TLS certificate validation for the Redis connection. Acceptable for local dev (Aspire's purpose), but must never be used for production Redis connections.
- If the project adds a production Redis deployment, the cert validation should be environment-conditional or use proper certificate trust.

## Impact
- WeatherApi project only (the sole Redis consumer via `IDistributedCache`).
- Worker service is unaffected — it talks to Redis indirectly through the WeatherApi HTTP endpoints.
