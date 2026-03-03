# Routing Rules

## Keyword Routing

| Keywords / Patterns | Route To |
|---------------------|----------|
| AppHost, Program.cs (AppHost), orchestration, service discovery, `WithReference`, `WaitFor`, `AddProject`, Aspire defaults, health checks, OpenTelemetry, service wiring | Holden |
| Redis, cache, caching, `IDistributedCache`, `AddRedis`, `StackExchangeRedis`, weather cache, cache expiry | Naomi |
| PostgreSQL, Postgres, database, EF Core, Entity Framework, migration, `AddPostgres`, `AddDatabase`, user preferences, saved cities, data model | Amos |
| Blazor, frontend, UI, dashboard, components, razor, pages, layout, city selector, forecast display, current conditions | Drummer |
| Worker, background, scheduled, timer, refresh, `BackgroundService`, `IHostedService`, periodic, 15-minute | Alex |
| Test, testing, integration test, e2e, validation, service discovery test, data flow test, health check test | Bobbie |
| Architecture, design, project structure, solution structure, cross-cutting | Holden (lead decision) |
| Simple bug fix, single-file edit, add endpoint, write docs, add test | @copilot (if issue is scoped + clear) |
| All services, full stack, team | Fan-out to relevant agents |

## Review Gates

| Artifact | Reviewer |
|----------|----------|
| AppHost Program.cs | Holden reviews all changes |
| Service discovery wiring | Holden reviews |
| New service projects | Holden reviews (must be wired in AppHost) |
| Database schema changes | Amos reviews |
| Cache key strategy | Naomi reviews |
| UI components | Drummer reviews |
| Test coverage | Bobbie reviews |
