using System.Diagnostics;
using System.Net.Http.Json;

namespace WeatherDashboard.Worker.Services;

public class WeatherRefreshWorker(
    IHttpClientFactory httpClientFactory,
    WeatherRefreshHealthCheck healthCheck,
    ILogger<WeatherRefreshWorker> logger) : BackgroundService
{
    private static readonly TimeSpan RefreshInterval = TimeSpan.FromMinutes(15);
    private static readonly TimeSpan CityStagger = TimeSpan.FromMilliseconds(500);
    private const int MaxRetries = 3;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(RefreshInterval);

        do
        {
            await RefreshAllCitiesAsync(stoppingToken);
        }
        while (await timer.WaitForNextTickAsync(stoppingToken));
    }

    private async Task RefreshAllCitiesAsync(CancellationToken ct)
    {
        var preferencesClient = httpClientFactory.CreateClient("preferencesapi");
        var weatherClient = httpClientFactory.CreateClient("weatherapi");

        List<string> cities;
        try
        {
            cities = await preferencesClient.GetFromJsonAsync<List<string>>("/api/preferences/cities", ct)
                ?? [];
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to fetch city list from Preferences API");
            healthCheck.RecordResult(0, 1, DateTimeOffset.UtcNow);
            return;
        }

        logger.LogInformation("Starting weather refresh cycle for {CityCount} cities", cities.Count);

        if (cities.Count == 0)
        {
            healthCheck.RecordResult(0, 0, DateTimeOffset.UtcNow);
            return;
        }

        var totalStopwatch = Stopwatch.StartNew();
        int successCount = 0;
        int failureCount = 0;

        for (int i = 0; i < cities.Count; i++)
        {
            var city = cities[i];
            var cityStopwatch = Stopwatch.StartNew();

            if (await RefreshCityWithRetryAsync(weatherClient, city, ct))
            {
                cityStopwatch.Stop();
                logger.LogInformation("Refreshed weather for {City} in {ElapsedMs}ms", city, cityStopwatch.ElapsedMilliseconds);
                successCount++;
            }
            else
            {
                cityStopwatch.Stop();
                failureCount++;
            }

            if (i < cities.Count - 1)
            {
                await Task.Delay(CityStagger, ct);
            }
        }

        totalStopwatch.Stop();
        logger.LogInformation(
            "Refresh cycle complete: {SuccessCount}/{TotalCount} cities in {TotalMs}ms",
            successCount, cities.Count, totalStopwatch.ElapsedMilliseconds);

        healthCheck.RecordResult(successCount, failureCount, DateTimeOffset.UtcNow);
    }

    private async Task<bool> RefreshCityWithRetryAsync(HttpClient weatherClient, string city, CancellationToken ct)
    {
        int delaySeconds = 1;

        for (int attempt = 0; attempt <= MaxRetries; attempt++)
        {
            try
            {
                var currentResponse = await weatherClient.GetAsync($"/api/weather/{Uri.EscapeDataString(city)}", ct);
                currentResponse.EnsureSuccessStatusCode();

                var forecastResponse = await weatherClient.GetAsync($"/api/weather/{Uri.EscapeDataString(city)}/forecast", ct);
                forecastResponse.EnsureSuccessStatusCode();

                return true;
            }
            catch (Exception ex) when (attempt < MaxRetries)
            {
                logger.LogWarning(ex, "Attempt {Attempt} failed for {City}, retrying in {DelaySeconds}s",
                    attempt + 1, city, delaySeconds);
                await Task.Delay(TimeSpan.FromSeconds(delaySeconds), ct);
                delaySeconds *= 2;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to refresh {City}: {Error}", city, ex.Message);
            }
        }

        return false;
    }
}
