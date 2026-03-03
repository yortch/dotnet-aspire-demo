using Microsoft.EntityFrameworkCore;
using WeatherDashboard.PreferencesApi.Data;
using WeatherDashboard.PreferencesApi.Endpoints;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();
builder.AddNpgsqlDbContext<PreferencesDbContext>("preferencesdb");

builder.Services.AddOpenApi();

// Health check for PostgreSQL connectivity (supplements auto-registered Aspire health check)
builder.Services.AddHealthChecks()
    .AddDbContextCheck<PreferencesDbContext>("preferencesdb");

var app = builder.Build();

app.MapDefaultEndpoints();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();

    // Auto-migrate in development
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<PreferencesDbContext>();
    await db.Database.MigrateAsync();
}

app.MapPreferencesEndpoints();

app.Run();