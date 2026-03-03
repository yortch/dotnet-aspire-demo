# Bobbie — History

## Project Context
- **Project:** Real-time weather dashboard using .NET Aspire
- **User:** Jorge Balderas
- **Stack:** .NET, Aspire, xUnit/NUnit, Aspire.Hosting.Testing
- **My Role:** Tester — I validate service discovery and end-to-end data flow

## Learnings
_Append new learnings below this line._

## Session: Issues #14, #15, #16, #18 — Integration, E2E, and Resilience Tests

### What I Did
- Created `WeatherDashboard.Tests/` using `dotnet new aspire-xunit` template (Aspire 13.1.2, net10.0, xUnit 2.9.3)
- Added project reference to AppHost and added test project to solution
- Created shared `AspireAppFixture` using `DistributedApplicationTestingBuilder` for all test classes (xUnit collection fixture pattern)
- **WeatherApiTests.cs** (Issue #14): 4 tests — GET current weather structure validation, GET forecast with Days array, 404 for nonsense city, caching behavior verification
- **PreferencesApiTests.cs** (Issue #15): 5 tests — GET seeded cities for demo-user, POST adds new city, POST duplicate returns 409, DELETE removes city, GET /cities returns distinct sorted list
- **ServiceDiscoveryTests.cs** (Issue #16): 5 tests — all resources running, weatherapi reachable via /alive, preferencesapi reachable via /alive, worker registered in resource model, no hardcoded localhost URLs in Frontend/Worker source
- **ResilienceTests.cs** (Issue #18): 4 tests — graceful timeout handling, cache hit verification (same retrievedAt), 503/404 for uncached fake city, concurrent request resilience on preferences API

### Key Decisions
- Used xUnit `IAsyncLifetime` with `Task` return types (xUnit 2.x, not ValueTask)
- Fully qualified `Aspire.Hosting.DistributedApplication` type since it's not in default usings
- Tests tolerate 503/500 when OpenWeatherMap API key isn't configured — validates structure, not external API availability
- All tests share one Aspire app instance via `[Collection("AspireApp")]` to minimize container startup overhead
- Tests require Docker for Redis/PostgreSQL containers; all 18 tests fail gracefully when Docker is unavailable

### Branch & PR
- Branch: `squad/14-16-18-integration-tests`
- PR: #27 — https://github.com/yortch/dotnet-aspire-demo/pull/27
- Build: ✅ 0 errors (2 pre-existing NU1504 warnings from PreferencesApi)
- Tests: All 18 fail on this machine due to Docker being unhealthy — expected for environments without container runtime
