# Naomi — History

## Project Context
- **Project:** Real-time weather dashboard using .NET Aspire
- **User:** Jorge Balderas
- **Stack:** .NET, Aspire, Redis, OpenWeatherMap API
- **My Role:** Redis Dev — I own the caching layer for weather data

## Learnings
_Append new learnings below this line._

- **Redis TLS self-signed cert fix (Aspire containers):** Aspire's Redis container uses a self-signed TLS certificate, which StackExchange.Redis rejects by default with `UntrustedRoot`. The fix is to use the `configureOptions` callback on `AddRedisDistributedCache` to accept the self-signed cert: `options.CertificateValidation += (_, _, _, _) => true;`. This is safe for local dev since Aspire orchestrates local containers only. The `CertificateValidation` event lives on `StackExchange.Redis.ConfigurationOptions` and is the documented Aspire approach (see MS Learn docs for `AddRedisDistributedCache` Aspire 13.x).
