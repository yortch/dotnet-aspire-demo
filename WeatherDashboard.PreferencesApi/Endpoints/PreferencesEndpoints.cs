using Microsoft.EntityFrameworkCore;
using WeatherDashboard.PreferencesApi.Data;
using WeatherDashboard.PreferencesApi.Models;

namespace WeatherDashboard.PreferencesApi.Endpoints;

public static class PreferencesEndpoints
{
    public static void MapPreferencesEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/preferences");

        group.MapGet("/cities", async (PreferencesDbContext db) =>
        {
            var cities = await db.UserPreferences
                .Select(p => p.City)
                .Distinct()
                .OrderBy(c => c)
                .ToListAsync();

            return Results.Ok(cities);
        })
        .WithName("GetAllCities");

        group.MapGet("/{userId}", async (string userId, PreferencesDbContext db) =>
        {
            var preferences = await db.UserPreferences
                .Where(p => p.UserId == userId)
                .OrderBy(p => p.DisplayOrder)
                .ToListAsync();

            return Results.Ok(preferences);
        })
        .WithName("GetUserPreferences");

        group.MapPost("/{userId}", async (string userId, AddCityRequest request, PreferencesDbContext db) =>
        {
            var exists = await db.UserPreferences
                .AnyAsync(p => p.UserId == userId && p.City == request.City);

            if (exists)
            {
                return Results.Conflict(new { message = $"City '{request.City}' is already saved for user '{userId}'." });
            }

            var maxOrder = await db.UserPreferences
                .Where(p => p.UserId == userId)
                .MaxAsync(p => (int?)p.DisplayOrder) ?? -1;

            var preference = new UserPreference
            {
                UserId = userId,
                City = request.City,
                DisplayOrder = maxOrder + 1,
                CreatedAt = DateTime.UtcNow
            };

            db.UserPreferences.Add(preference);
            await db.SaveChangesAsync();

            return Results.Created($"/api/preferences/{userId}", preference);
        })
        .WithName("AddUserPreference");

        group.MapDelete("/{userId}/{city}", async (string userId, string city, PreferencesDbContext db) =>
        {
            var preference = await db.UserPreferences
                .FirstOrDefaultAsync(p => p.UserId == userId && p.City == city);

            if (preference is null)
            {
                return Results.NotFound();
            }

            db.UserPreferences.Remove(preference);
            await db.SaveChangesAsync();

            return Results.NoContent();
        })
        .WithName("DeleteUserPreference");
    }
}

public record AddCityRequest(string City);
