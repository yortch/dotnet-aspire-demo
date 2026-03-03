using System.Text.Json;
using Microsoft.Extensions.Caching.Distributed;
using WeatherDashboard.WeatherApi.Models;

namespace WeatherDashboard.WeatherApi.Services;

public class WeatherCacheService(IDistributedCache cache, ILogger<WeatherCacheService> logger)
{
    private static readonly DistributedCacheEntryOptions CacheOptions = new()
    {
        AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(15)
    };

    private static string CurrentKey(string city) => $"weather:current:{OpenWeatherMapService.NormalizeCity(city)}";
    private static string ForecastKey(string city) => $"weather:forecast:{OpenWeatherMapService.NormalizeCity(city)}";

    public async Task<CurrentWeather> GetOrSetCurrentWeatherAsync(string city, Func<Task<CurrentWeather>> factory)
    {
        var key = CurrentKey(city);
        return await GetOrSetAsync(key, factory);
    }

    public async Task<Forecast> GetOrSetForecastAsync(string city, Func<Task<Forecast>> factory)
    {
        var key = ForecastKey(city);
        return await GetOrSetAsync(key, factory);
    }

    private async Task<T> GetOrSetAsync<T>(string key, Func<Task<T>> factory) where T : class
    {
        // Try cache first
        var cached = await cache.GetStringAsync(key);
        if (cached is not null)
        {
            var deserialized = JsonSerializer.Deserialize<T>(cached);
            if (deserialized is not null)
            {
                logger.LogDebug("Cache hit for {Key}", key);
                return deserialized;
            }
        }

        // Call factory (OpenWeatherMap)
        try
        {
            var result = await factory();
            var json = JsonSerializer.Serialize(result);
            await cache.SetStringAsync(key, json, CacheOptions);
            logger.LogDebug("Cache set for {Key}", key);
            return result;
        }
        catch (HttpRequestException ex)
        {
            // On API failure, return stale cache if available
            if (cached is not null)
            {
                logger.LogWarning(ex, "OpenWeatherMap unavailable, returning stale cache for {Key}", key);
                return JsonSerializer.Deserialize<T>(cached)!;
            }

            throw;
        }
    }
}
