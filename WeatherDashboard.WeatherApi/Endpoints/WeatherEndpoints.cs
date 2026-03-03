using WeatherDashboard.WeatherApi.Services;

namespace WeatherDashboard.WeatherApi.Endpoints;

public static class WeatherEndpoints
{
    public static void MapWeatherEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/weather");

        group.MapGet("/{city}", async (string city, WeatherCacheService cacheService, OpenWeatherMapService weatherService) =>
        {
            try
            {
                var weather = await cacheService.GetOrSetCurrentWeatherAsync(city,
                    () => weatherService.GetCurrentWeatherAsync(city));
                return Results.Ok(weather);
            }
            catch (KeyNotFoundException)
            {
                return Results.NotFound(new { error = $"City '{city}' not found." });
            }
            catch (HttpRequestException)
            {
                return Results.StatusCode(503);
            }
        })
        .WithName("GetCurrentWeather");

        group.MapGet("/{city}/forecast", async (string city, WeatherCacheService cacheService, OpenWeatherMapService weatherService) =>
        {
            try
            {
                var forecast = await cacheService.GetOrSetForecastAsync(city,
                    () => weatherService.GetForecastAsync(city));
                return Results.Ok(forecast);
            }
            catch (KeyNotFoundException)
            {
                return Results.NotFound(new { error = $"City '{city}' not found." });
            }
            catch (HttpRequestException)
            {
                return Results.StatusCode(503);
            }
        })
        .WithName("GetForecast");
    }
}
