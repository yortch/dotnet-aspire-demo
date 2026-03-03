var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();
builder.AddNpgsqlDbContext<PreferencesDbContext>("preferencesdb");

builder.Services.AddOpenApi();

var app = builder.Build();

app.MapDefaultEndpoints();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.MapGet("/preferences", () =>
{
    return new[] { new Preference("temperature-unit", "celsius"), new Preference("theme", "dark") };
})
.WithName("GetPreferences");

app.Run();

record Preference(string Key, string Value);

// Placeholder DbContext for PostgreSQL integration
public class PreferencesDbContext : Microsoft.EntityFrameworkCore.DbContext
{
    public PreferencesDbContext(Microsoft.EntityFrameworkCore.DbContextOptions<PreferencesDbContext> options)
        : base(options) { }
}
